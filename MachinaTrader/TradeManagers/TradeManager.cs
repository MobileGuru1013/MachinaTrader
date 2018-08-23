using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Strategies;

namespace MachinaTrader.TradeManagers
{
    public class TradeManager : ITradeManager
    {
        #region BUY SIDE

        /// <summary>
        /// Checks if new trades can be started.
        /// </summary>
        /// <returns></returns>
        public async Task LookForNewTrades(string strategyString = null)
        {
            ITradingStrategy strategy;

            if (strategyString != null)
            {
                var type = Type.GetType($"MachinaTrader.Strategies.{strategyString}, MachinaTrader.Strategies", true, true);
                strategy = Activator.CreateInstance(type) as ITradingStrategy ?? new TheScalper();
            }
            else
            {
                var type = Type.GetType($"MachinaTrader.Strategies.{Global.Configuration.TradeOptions.DefaultStrategy}, MachinaTrader.Strategies", true, true);
                strategy = Activator.CreateInstance(type) as ITradingStrategy ?? new TheScalper();
            }

            //Global.Logger.Information($"Looking for trades using {strategy.Name}");

            // Check active trades against our strategy.
            await SellActiveTrades(strategy);

            await FindBuyOpportunities(strategy);
        }

        /// <summary>
        /// Cancels any orders that have been buying for an entire cycle.
        /// </summary>
        /// <returns></returns>
        public async Task CancelUnboughtOrders()
        {
            //Global.Logger.Information($"Starting CancelUnboughtOrders");

            // Only trigger if there are orders still buying.
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();

            // Loop our current trades that are still looking to buy if there are any.
            foreach (var trade in activeTrades.Where(x => x.IsBuying))
            {
                // Only in livetrading
                if (!Global.Configuration.TradeOptions.PaperTrade)
                {
                    // Cancel our open buy order on the exchange.
                    var exchangeOrder = await Global.ExchangeApi.GetOrder(trade.BuyOrderId, trade.Market);

                    // If this order is PartiallyFilled, don't cancel
                    if (exchangeOrder?.Status == OrderStatus.PartiallyFilled)
                        continue;  // not yet completed so wait

                    await Global.ExchangeApi.CancelOrder(trade.BuyOrderId, trade.Market);
                }

                trade.OpenOrderId = null;
                trade.BuyOrderId = null;
                trade.SellOrderId = null;
                trade.IsOpen = false;
                trade.IsBuying = false;
                trade.SellType = SellType.Cancelled;
                trade.CloseDate = DateTime.UtcNow;

                await Global.DataStore.SaveTradeAsync(trade);

                await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                {
                    Amount = (trade.OpenRate * trade.Quantity),
                    Date = DateTime.UtcNow
                });

                await SendNotification($"Buy Order cancelled because it wasn't filled in time: {this.TradeToString(trade)}.");
            }
        }

        /// <summary>
        /// Checks our current running trades against the strategy, profit and manual actions
        /// </summary>
        /// <returns></returns>
        private async Task SellActiveTrades(ITradingStrategy strategy)
        {
            var activeTrades = Global.DataStore.GetActiveTradesAsync().Result.Where(x => !x.IsSelling);  //so IsBuying (pending) and isOpen

            //Global.Logger.Information($"Starting SellActiveTradesAgainstStrategies, check {activeTrades.Count()} orders");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            foreach (var trade in activeTrades)
            {
                var currentProfit = (trade.TickerLast.Bid - trade.OpenRate) / trade.OpenRate;

                // Sell if we setup instant sell, manually by UI
                if (trade.SellNow)
                {
                    var orderId = Global.Configuration.TradeOptions.PaperTrade ? GetOrderId() : await Global.ExchangeApi.Sell(trade.Market, trade.Quantity, trade.TickerLast.Bid);

                    if (orderId == null)
                    {
                        await SendNotification($"Error to open a Selling Order by manually set: profit {currentProfit} for {this.TradeToString(trade)}");
                        continue;
                    }

                    trade.OpenOrderId = orderId;
                    trade.SellOrderId = orderId;
                    trade.IsSelling = true;
                    trade.CloseRate = trade.TickerLast.Bid;
                    trade.SellType = SellType.Manually;

                    await Global.DataStore.SaveTradeAsync(trade);

                    await SendNotification($"Opened a Selling Order by manually set: profit {currentProfit} for {this.TradeToString(trade)}");

                    continue;
                }

                // Hold
                if (trade.HoldPosition)
                {
                    await SendNotification($"Hold is set: profit {currentProfit} for {this.TradeToString(trade)}");
                    continue;
                }

                var profitIsOverSellOnPercentage = (trade.SellOnPercentage > 0 && (currentProfit >= (trade.SellOnPercentage / 100)));
                var profitIsOverSellOrderAtProfit = (Global.Configuration.TradeOptions.ImmediatelyPlaceSellOrder && (currentProfit >= Global.Configuration.TradeOptions.ImmediatelyPlaceSellOrderAtProfit));

                // Sell if defined percentage is reached
                if (profitIsOverSellOrderAtProfit || profitIsOverSellOnPercentage)
                {
                    var orderId = Global.Configuration.TradeOptions.PaperTrade ? GetOrderId() : await Global.ExchangeApi.Sell(trade.Market, trade.Quantity, trade.TickerLast.Bid);

                    trade.OpenOrderId = orderId;
                    trade.SellOrderId = orderId;
                    trade.IsSelling = true;
                    trade.CloseRate = trade.TickerLast.Bid;
                    trade.SellType = SellType.Immediate;

                    await Global.DataStore.SaveTradeAsync(trade);

                    await SendNotification($"Opened a Selling Order by reached defined percentage: profit {currentProfit} for {this.TradeToString(trade)}");

                    continue;
                }

                //Global.Logger.Information($"Checking sell signal for {this.TradeToString(trade)}");

                var signal = await GetStrategySignal(trade.Market, strategy);

                // If the strategy is telling us to sell we need to do so.
                if (signal?.TradeAdvice == TradeAdvice.Sell)
                {
                    // Create a sell order for our strategy.
                    var ticker = await Global.ExchangeApi.GetTicker(trade.Market);

                    // Check Trading Mode
                    var orderId = Global.Configuration.TradeOptions.PaperTrade ? GetOrderId() : await Global.ExchangeApi.Sell(trade.Market, trade.Quantity, ticker.Bid);

                    trade.OpenOrderId = orderId;
                    trade.SellOrderId = orderId;
                    trade.IsSelling = true;
                    trade.CloseRate = ticker.Bid;
                    trade.SellType = SellType.Strategy;

                    await Global.DataStore.SaveTradeAsync(trade);

                    await SendNotification($"Opened a Selling Order by signal: profit {currentProfit.ToString("p1")} for {this.TradeToString(trade)}");
                }
            }
            //watch1.Stop();
            //Global.Logger.Warning($"Ended SellActiveTradesAgainstStrategies, checked {activeTrades.Count()} orders in #{watch1.Elapsed.TotalSeconds} seconds");
        }

        /// <summary>
        /// Checks the implemented trading indicator(s),
        /// if one pair triggers the buy signal a new trade record gets created.
        /// </summary>
        /// <returns></returns>
        private async Task<List<TradeSignal>> FindBuyOpportunities(ITradingStrategy strategy)
        {
            //Global.Logger.Information($"Starting FindBuyOpportunities");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            var pairs = new List<TradeSignal>();

            // Retrieve our exchange current markets
            var markets = await Global.ExchangeApi.GetMarketSummaries(Global.Configuration.TradeOptions.QuoteCurrency);

            //Global.Logger.Information($"Market of exchange {Global.ExchangeApi.GetFullApi().Result.Name}: {markets.Count()}");

            // Check if there are markets matching our volume.
            markets = markets.Where(x =>
                (x.Volume > Global.Configuration.TradeOptions.MinimumAmountOfVolume ||
                 Global.Configuration.TradeOptions.AlwaysTradeList.Contains(x.CurrencyPair.BaseCurrency)) &&
                 Global.Configuration.TradeOptions.QuoteCurrency.ToUpper() == x.CurrencyPair.QuoteCurrency.ToUpper()).ToList();

            // If there are items on the only trade list remove the rest
            if (Global.Configuration.TradeOptions.OnlyTradeList.Count > 0)
                markets = markets.Where(m => Global.Configuration.TradeOptions.OnlyTradeList.Any(c => c.Contains(m.CurrencyPair.BaseCurrency))).ToList();

            // Remove existing trades from the list to check.
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            foreach (var trade in activeTrades)
                markets.RemoveAll(x => x.MarketName == trade.Market);

            // Remove items that are on our blacklist.
            foreach (var market in Global.Configuration.TradeOptions.MarketBlackList)
                markets.RemoveAll(x => x.MarketName == market);

            #region test Buy from external - Currently for Debug -> This will buy on each tick !
            /******************************/
            //var externalTicker = await Global.ExchangeApi.GetTicker("LINKBTC");
            //Candle externalCandle = new Candle();
            //externalCandle.Timestamp = DateTime.UtcNow;
            //externalCandle.Open = externalTicker.Last;
            //externalCandle.High = externalTicker.Last;
            //externalCandle.Volume = externalTicker.Volume;
            //externalCandle.Close = externalTicker.Last;
            //pairs.Add(new TradeSignal
            //{
            //    MarketName = "LINKBTC",
            //    QuoteCurrency = "LINK",
            //    BaseCurrency = "BTC",
            //    TradeAdvice = TradeAdvice.StrongBuy,
            //    SignalCandle = externalCandle
            //});

            //_activeTrades = await Global.DataStore.GetActiveTradesAsync();
            //if (_activeTrades.Where(x => x.IsOpen).Count() < Global.Configuration.TradeOptions.MaxNumberOfConcurrentTrades)
            //{
            //    await CreateNewTrade(new TradeSignal
            //    {
            //        MarketName = "LINKBTC",
            //        QuoteCurrency = "LINK",
            //        BaseCurrency = "BTC",
            //        TradeAdvice = TradeAdvice.StrongBuy,
            //        SignalCandle = externalCandle
            //    }, strategy);
            //    //Global.Logger.Information("Match signal -> Buying " + "LINKBTC");
            //}
            //else
            //{
            //    //Global.Logger.Information("Too Many Trades: Ignore Match signal " + "LINKBTC");
            //}
            /******************************/
            #endregion

            foreach (var market in markets.Distinct().OrderByDescending(x => x.Volume).ToList())
            {
                var signal = await GetStrategySignal(market.MarketName, strategy);

                // A match was made, buy that please!
                if (signal?.TradeAdvice == TradeAdvice.Buy)
                {
                    //Global.Logger.Information($"Found BUY SIGNAL {signal.SignalCandle.Timestamp} for: {market.MarketName} at {signal.SignalCandle.Close} {Global.Configuration.TradeOptions.QuoteCurrency}");

                    if (activeTrades.Count() < Global.Configuration.TradeOptions.MaxNumberOfConcurrentTrades)
                    {
                        await CreateNewTrade(new TradeSignal
                        {
                            MarketName = market.MarketName,
                            QuoteCurrency = market.CurrencyPair.QuoteCurrency,
                            BaseCurrency = market.CurrencyPair.BaseCurrency,
                            TradeAdvice = signal.TradeAdvice,
                            SignalCandle = signal.SignalCandle
                        }, strategy);
                    }
                    else
                    {
                        Global.Logger.Information("Too Many Trades: Ignore Match signal " + market.MarketName);
                    }
                }
            };

            //watch1.Stop();
            //Global.Logger.Warning($"Ended FindBuyOpportunities in #{watch1.Elapsed.TotalSeconds} seconds");

            return pairs;
        }

        /// <summary>
        /// Calculates a buy signal based on several technical analysis indicators.
        /// </summary>
        /// <param name="market">The market we're going to check against.</param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        private async Task<TradeSignal> GetStrategySignal(string market, ITradingStrategy strategy)
        {
            //Global.Logger.Information($"Starting GetStrategySignal {market}");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            var minimumDate = strategy.GetMinimumDateTime();
            var candleDate = strategy.GetCurrentCandleDateTime();
            DateTime? endDate = null;

            if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation)
            {
                //in simulation the date comes from external
                candleDate = Global.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate;

                //TODO: improve to other timeframe
                minimumDate = candleDate.AddMinutes(-(30 * strategy.MinimumAmountOfCandles));

                endDate = candleDate;
            }

            var candles = await Global.ExchangeApi.GetTickerHistory(market, strategy.IdealPeriod, minimumDate, endDate);

            var desiredLastCandleTime = candleDate.AddMinutes(-(strategy.IdealPeriod.ToMinutesEquivalent()));

            //Global.Logger.Information("Checking signal for market {Market} lastCandleTime {a} - desiredLastCandleTime {b}", market, candles.Last().Timestamp, desiredLastCandleTime);

            if (!candles.Any())
                return null;

            int k = 1;

            //on simulation, if we dont have candles we have to re-check our DB data..
            while (candles.Last().Timestamp < desiredLastCandleTime && k < 20 && !Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation)
            {
                k++;
                Thread.Sleep(1000 * k);

                candles = await Global.ExchangeApi.GetTickerHistory(market, strategy.IdealPeriod, minimumDate, endDate);
                Global.Logger.Information("R Checking signal for market {Market} lastCandleTime {a} - desiredLastCandleTime {b}", market, candles.Last().Timestamp, desiredLastCandleTime);
            }

            //Global.Logger.Information("Checking signal for market {Market} lastCandleTime: {last} , close: {close}", market, candles.Last().Timestamp, candles.Last().Close);

            if (!candles.Any())
                return null;

            // We eliminate all candles that aren't needed for the dataset incl. the last one (if it's the current running candle).
            candles = candles.Where(x => x.Timestamp >= minimumDate && x.Timestamp < candleDate).ToList();

            // Not enough candles to perform what we need to do.
            if (candles.Count < strategy.MinimumAmountOfCandles)
            {
                //Global.Logger.Warning("Not enough candle data for {Market}...", market);
                return new TradeSignal
                {
                    TradeAdvice = TradeAdvice.Hold,
                    MarketName = market
                };
            }

            // Get the date for the last candle.
            var signalDate = candles[candles.Count - 1].Timestamp;
            var strategySignalDate = strategy.GetSignalDate();

            if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation)
            {
                //TODO: improve to other timeframe
                strategySignalDate = candleDate.AddMinutes(-30);
            }

            // This is an outdated candle...
            if (signalDate < strategySignalDate)
            {
                Global.Logger.Information("Outdated candle for {Market}...", market);
                return null;
            }

            // This calculates an advice for the next timestamp.
            var advice = strategy.Forecast(candles);

            //watch1.Stop();
            //Global.Logger.Warning($"Ended FindBuyOpportunities in #{watch1.Elapsed.TotalSeconds} seconds");

            return new TradeSignal
            {
                TradeAdvice = advice,
                MarketName = market,
                SignalCandle = strategy.GetSignalCandle(candles)
            };
        }

        /// <summary>
        /// Calculates bid target between current ask price and last price.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="signalCandle"></param>
        /// <returns></returns>
        private decimal GetTargetBid(Ticker tick, Candle signalCandle)
        {
            if (Global.Configuration.TradeOptions.BuyInPriceStrategy == BuyInPriceStrategy.AskLastBalance)
            {
                // If the ask is below the last, we can get it on the cheap.
                if (tick.Ask < tick.Last)
                    return tick.Ask;

                return tick.Ask + Global.Configuration.TradeOptions.AskLastBalance * (tick.Last - tick.Ask);
            }

            if (Global.Configuration.TradeOptions.BuyInPriceStrategy == BuyInPriceStrategy.SignalCandleClose)
            {
                return signalCandle.Close;
            }

            if (Global.Configuration.TradeOptions.BuyInPriceStrategy == BuyInPriceStrategy.MatchCurrentBid)
            {
                return tick.Bid;
            }

            return Math.Round(tick.Bid * (1 - Global.Configuration.TradeOptions.BuyInPricePercentage), 8);
        }

        /// <summary>
        /// Creates a new trade in our system and opens a buy order.
        /// </summary>
        /// <returns></returns>
        private async Task CreateNewTrade(TradeSignal signal, ITradingStrategy strategy)
        {
            decimal currentQuoteBalance = 9999;

            if (!Global.Configuration.TradeOptions.PaperTrade)
            {
                // Get our Bitcoin balance from the exchange
                var exchangeQuoteBalance = await Global.ExchangeApi.GetBalance(signal.QuoteCurrency);

                // Check trading mode
                currentQuoteBalance = exchangeQuoteBalance.Available;
            }

            // Do we even have enough funds to invest? (only for SetAside; if you choose Reinvest we push a %) 
            if (Global.Configuration.TradeOptions.ProfitStrategy == ProfitType.SetAside && currentQuoteBalance < Global.Configuration.TradeOptions.AmountToInvestPerTrader)
            {
                Global.Logger.Warning("Insufficient funds ({Available}) to perform a {MarketName} trade. Skipping this trade.", currentQuoteBalance, signal.MarketName);
                return;
            }

            var trade = await CreateBuyOrder(signal, strategy);

            // We found a trade and have set it all up!
            if (trade != null)
            {
                // Save the order.
                await Global.DataStore.SaveTradeAsync(trade);

                //money area unavailable from wallet immediately
                await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                {
                    Amount = -(trade.OpenRate * trade.Quantity),
                    Date = trade.OpenDate
                });

                // Send a notification that we found something suitable
                await SendNotification($"Saved a BUY ORDER for: {this.TradeToString(trade)}");
            }
        }

        /// <summary>
        /// Creates a buy order on the exchange.
        /// </summary>
        /// <param name="pair">The pair we're buying</param>
        /// <param name="signalCandle"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        private async Task<Trade> CreateBuyOrder(TradeSignal signal, ITradingStrategy strategy)
        {
            var pair = signal.MarketName;
            var signalCandle = signal.SignalCandle;

            // Take the amount to invest per trader OR the current balance for this trader.
            var fichesToSpend = Global.Configuration.TradeOptions.AmountToInvestPerTrader;

            if (Global.Configuration.TradeOptions.ProfitStrategy == ProfitType.Reinvest)
            {
                var exchangeQuoteBalance = Global.ExchangeApi.GetBalance(signal.QuoteCurrency).Result.Available;
                fichesToSpend = exchangeQuoteBalance * Global.Configuration.TradeOptions.AmountToReinvestPercentage / 100;
            }

            // The amount here is an indication and will probably not be precisely what you get.
            var ticker = await Global.ExchangeApi.GetTicker(pair);
            var openRate = GetTargetBid(ticker, signalCandle);
            var amount = fichesToSpend / openRate;

            // Get the order ID, this is the most important because we need this to check
            // up on our trade. We update the data below later when the final data is present.
            var orderId = Global.Configuration.TradeOptions.PaperTrade ? GetOrderId() : await Global.ExchangeApi.Buy(pair, amount, openRate);

            if (orderId == null)
            {
                Global.Logger.Error($"Error to open a BUY Order for: {pair} {amount} {openRate}");
                return null;
            }

            var fullApi = await Global.ExchangeApi.GetFullApi();
            var symbol = await Global.ExchangeApi.ExchangeCurrencyToGlobalCurrency(pair);

            var trade = new Trade()
            {
                Market = pair,
                StakeAmount = fichesToSpend,
                OpenRate = openRate,
                OpenDate = signalCandle.Timestamp,
                Quantity = amount,
                OpenOrderId = orderId,
                BuyOrderId = orderId,
                SellOrderId = null,
                IsOpen = true,
                IsBuying = true,
                IsSelling = false,
                StrategyUsed = strategy.Name,
                SellType = SellType.None,
                TickerLast = ticker,
                GlobalSymbol = symbol,
                Exchange = fullApi.Name,
                PaperTrade = Global.Configuration.TradeOptions.PaperTrade
            };

            if (Global.Configuration.TradeOptions.PlaceFirstStopAtSignalCandleLow)
            {
                trade.StopLossRate = signalCandle.Low;
                Global.Logger.Information("Automatic stop set at signal candle low {Low}", signalCandle.Low.ToString("0.00000000"));
            }

            //Global.Logger.Information($"Opened a BUY Order for: {this.TradeToString(trade)}");

            return trade;
        }

        #endregion

        #region SELL SIDE

        public async Task UpdateExistingTrades()
        {
            // First we update our open buy orders by checking if they're filled.
            await UpdateOpenBuyOrders();

            // Secondly we check if currently selling trades can be marked as sold if they're filled.
            await UpdateOpenSellOrders();

            // Third, our current trades need to be checked if one of these has hit its sell targets...
            if (!Global.Configuration.TradeOptions.OnlySellOnStrategySignals)
            {
                await CheckForSellConditions();
            }

            // This means an order to buy has been open for an entire buy cycle.
            if (Global.Configuration.TradeOptions.CancelUnboughtOrdersEachCycle && Runtime.GlobalOrderBehavior == OrderBehavior.CheckMarket)
                await new TradeManager().CancelUnboughtOrders();
        }

        /// <summary>
        /// Updates the buy orders by checking with the exchange what status they are currently.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateOpenBuyOrders()
        {
            //Global.Logger.Information($"Starting UpdateOpenBuyOrders");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            // This means its a buy trade that is waiting to get bought. See if we can update that first.
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();

            foreach (var trade in activeTrades.Where(x => x.IsBuying))
            {
                //Global.Logger.Information($"Checking Opened BUY Order {this.TradeToString(trade)}");

                if (Global.Configuration.TradeOptions.PaperTrade)
                {
                    //in simulation mode we always fill..
                    if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation)
                    {
                        trade.OpenOrderId = null;
                        trade.IsBuying = false;
                    }
                    else
                    {
                        // Papertrading
                        var candles = await Global.ExchangeApi.GetTickerHistory(trade.Market, Period.Minute, 1);
                        var candle = candles.LastOrDefault();

                        if (candle != null && (trade.OpenRate >= candle.High ||
                                               (trade.OpenRate >= candle.Low && trade.OpenRate <= candle.High) ||
                                               Runtime.GlobalOrderBehavior == OrderBehavior.AlwaysFill
                            ))
                        {
                            trade.OpenOrderId = null;
                            trade.IsBuying = false;
                        }
                    }

                    await SendNotification($"BUY Order is filled: {this.TradeToString(trade)}");
                }
                else
                {
                    // Livetrading
                    var exchangeOrder = await Global.ExchangeApi.GetOrder(trade.BuyOrderId, trade.Market);

                    // if this order is filled, we can update our database.
                    if (exchangeOrder?.Status == OrderStatus.Filled)
                    {
                        trade.OpenOrderId = null;
                        trade.IsBuying = false;
                        trade.StakeAmount = exchangeOrder.OriginalQuantity * exchangeOrder.Price;
                        trade.Quantity = exchangeOrder.OriginalQuantity;
                        trade.OpenRate = exchangeOrder.Price;
                        trade.OpenDate = Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation ? trade.OpenDate : exchangeOrder.OrderDate;

                        await SendNotification($"BUY Order is filled: {this.TradeToString(trade)}");
                    }
                }

                await Global.DataStore.SaveTradeAsync(trade);

                //watch1.Stop();
                //Global.Logger.Warning($"Ended UpdateOpenBuyOrders in #{watch1.Elapsed.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Updates the sell orders by checking with the exchange what status they are currently.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateOpenSellOrders()
        {
            //Global.Logger.Information($"Starting UpdateOpenSellOrders");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            // There are trades that have an open order ID set & sell order id set
            // that means its a sell trade that is waiting to get sold. See if we can update that first.

            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            foreach (var trade in activeTrades.Where(x => x.IsSelling))
            {
                //Global.Logger.Information($"Checking Opened SELL Order {this.TradeToString(trade)}");

                if (Global.Configuration.TradeOptions.PaperTrade)
                {
                    List<Candle> candles;

                    if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation)
                    {
                        //in simulation the date comes from external
                        var simulationCurrentDate = Global.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate;

#warning //TODO: improve to other timeframe
                        DateTime startDate = simulationCurrentDate.AddMinutes(-(30 * 40));
                        DateTime endDate = simulationCurrentDate;

                        candles = await Global.ExchangeApi.GetTickerHistory(trade.Market, Period.Minute, startDate, endDate);
                    }
                    else
                    {
                        candles = await Global.ExchangeApi.GetTickerHistory(trade.Market, Period.Minute, 1);
                    }

                    var candle = candles.LastOrDefault();

                    //in simulation mode we always fill..
                    if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation)
                    {
                        trade.OpenOrderId = null;
                        trade.IsOpen = false;
                        trade.IsSelling = false;
                        trade.CloseDate = candle.Timestamp;
                        trade.CloseProfit = (trade.CloseRate * trade.Quantity) - trade.StakeAmount;
                        trade.CloseProfitPercentage = ((trade.CloseRate * trade.Quantity) - trade.StakeAmount) / trade.StakeAmount * 100;
                    }
                    else
                    {
                        if (candle != null && (trade.CloseRate <= candle.Low || (trade.CloseRate >= candle.Low && trade.CloseRate <= candle.High) || Runtime.GlobalOrderBehavior == OrderBehavior.AlwaysFill))
                        {
                            trade.OpenOrderId = null;
                            trade.IsOpen = false;
                            trade.IsSelling = false;
                            trade.CloseDate = DateTime.UtcNow;
                            trade.CloseProfit = (trade.CloseRate * trade.Quantity) - trade.StakeAmount;
                            trade.CloseProfitPercentage = ((trade.CloseRate * trade.Quantity) - trade.StakeAmount) / trade.StakeAmount * 100;
                        }
                    }

                    await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                    {
                        Amount = (trade.CloseRate.Value * trade.Quantity),
                        Date = DateTime.UtcNow
                    });

                    await SendNotification($"Sell Order is filled: {this.TradeToString(trade)}");
                }
                else
                {
                    // Livetrading

                    if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation)
                    {
                        //in simulation the date comes from external
                        var simulationCurrentDate = Global.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate;

#warning //TODO: improve to other timeframe
                        DateTime startDate = simulationCurrentDate.AddMinutes(-(30 * 40));
                        DateTime endDate = simulationCurrentDate;

                        var candles = await Global.ExchangeApi.GetTickerHistory(trade.Market, Period.Minute, startDate, endDate);
                        var candle = candles.LastOrDefault();

                        trade.OpenOrderId = null;
                        trade.IsOpen = false;
                        trade.IsSelling = false;
                        trade.CloseDate = candle.Timestamp;
                        trade.CloseProfit = (trade.CloseRate * trade.Quantity) - trade.StakeAmount;
                        trade.CloseProfitPercentage = ((trade.CloseRate * trade.Quantity) - trade.StakeAmount) / trade.StakeAmount * 100;

                        await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                        {
                            Amount = (trade.CloseRate.Value * trade.Quantity),
                            Date = trade.CloseDate.Value
                        });

                        await SendNotification($"Sell Order is filled: {this.TradeToString(trade)}");
                    }
                    else
                    {
                        var exchangeOrder = await Global.ExchangeApi.GetOrder(trade.SellOrderId, trade.Market);

                        // if this order is filled, we can update our database.
                        if (exchangeOrder?.Status == OrderStatus.Filled)
                        {
                            trade.OpenOrderId = null;
                            trade.IsOpen = false;
                            trade.IsSelling = false;
                            trade.CloseDate = exchangeOrder.OrderDate;
                            trade.CloseRate = exchangeOrder.Price;
                            trade.Quantity = exchangeOrder.ExecutedQuantity;
                            trade.CloseProfit = (exchangeOrder.Price * exchangeOrder.ExecutedQuantity) - trade.StakeAmount;
                            trade.CloseProfitPercentage = ((exchangeOrder.Price * exchangeOrder.ExecutedQuantity) - trade.StakeAmount) / trade.StakeAmount * 100;

                            await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                            {
                                Amount = (trade.CloseRate.Value * trade.Quantity),
                                Date = trade.CloseDate.Value
                            });

                            await SendNotification($"Sell Order is filled: {this.TradeToString(trade)}");
                        }
                    }
                }

                await Global.DataStore.SaveTradeAsync(trade);

                //watch1.Stop();
                //Global.Logger.Warning($"Ended UpdateOpenSellOrders in #{watch1.Elapsed.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Checks the current active trades if they need to be sold.
        /// </summary>
        /// <returns></returns>
        private async Task CheckForSellConditions()
        {
            //Global.Logger.Information($"Starting CheckForSellConditions");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            // There are trades that have no open order ID set & are still open.
            // that means its a trade that is waiting to get sold. See if we can update that first.

            // An open order currently not selling or being an immediate sell are checked for SL  etc.
            // Prioritize markets with high volume.
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            foreach (var trade in activeTrades.Where(x => !x.IsSelling && !x.IsBuying))
            {
                //Global.Logger.Information($"Checking sell conditions for Opened position: {this.TradeToString(trade)}");

                // These are trades that are not being bought or sold at the moment so these need to be checked for sell conditions.
                var ticker = Global.ExchangeApi.GetTicker(trade.Market).Result;
                var sellType = await ShouldSell(trade, ticker, DateTime.UtcNow);

                if (sellType == SellType.TrailingStopLossUpdated)
                {
                    // Update the stop loss for this trade, which was set in ShouldSell.
                    await Global.DataStore.SaveTradeAsync(trade);
                }
                else if (sellType != SellType.None)
                {
                    var orderId = Global.Configuration.TradeOptions.PaperTrade ? GetOrderId() : await Global.ExchangeApi.Sell(trade.Market, trade.Quantity, ticker.Bid);

                    trade.CloseRate = trade.TickerLast.Bid;
                    trade.OpenOrderId = orderId;
                    trade.SellOrderId = orderId;
                    trade.SellType = sellType;
                    trade.IsSelling = true;

                    await Global.DataStore.SaveTradeAsync(trade);

                    await SendNotification($"Selling trade for {trade.SellType} {this.TradeToString(trade)}");
                }
            }

            //watch1.Stop();
            //Global.Logger.Warning($"Ended CheckForSellConditions in #{watch1.Elapsed.TotalSeconds} seconds");
        }

        /// <summary>
        /// Based on earlier trade and current price and configuration, decides whether bot should sell.
        /// </summary>
        /// <param name="trade"></param>
        /// <param name="ticker"></param>
        /// <param name="utcNow"></param>
        /// <returns>True if bot should sell at current rate.</returns>
        private async Task<SellType> ShouldSell(Trade trade, Ticker ticker, DateTime utcNow)
        {
            var currentProfit = (ticker.Bid - trade.OpenRate) / trade.OpenRate;

            //Global.Logger.Information($"Should sell? profit {(currentProfit * 100).ToString("0.00")} {this.TradeToString(trade)}");

            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            var tradeToUpdate = activeTrades.FirstOrDefault(x => x.TradeId == trade.TradeId);
            if (tradeToUpdate != null)
            {
                tradeToUpdate.TickerLast = ticker;
                await Global.DataStore.SaveTradeAsync(tradeToUpdate);
            }

            // Let's not do a stoploss for now...
            if (currentProfit < Global.Configuration.TradeOptions.StopLossPercentage)
            {
                Global.Logger.Information("Stop loss hit: {StopLoss}%", Global.Configuration.TradeOptions.StopLossPercentage);
                return SellType.StopLoss;
            }

            // Only use ROI when no stoploss is set, because the stop loss
            // will be the anchor that sells when the trade falls below it.
            // This gives the trade room to rise further instead of selling directly.
            if (!trade.StopLossRate.HasValue)
            {
                // Check if time matches and current rate is above threshold
                foreach (var item in Global.Configuration.TradeOptions.ReturnOnInvestment)
                {
                    var timeDiff = (utcNow - trade.OpenDate).TotalSeconds / 60;

                    if (timeDiff > item.Duration && currentProfit > item.Profit)
                    {
                        Global.Logger.Information("Timer hit: {TimeDifference} mins, profit {Profit}%", timeDiff, item.Profit.ToString("0.00"));
                        return SellType.Timed;
                    }
                }
            }

            // Only run this when we're past our starting percentage for trailing stop.
            if (Global.Configuration.TradeOptions.EnableTrailingStop)
            {
                // If the current rate is below our current stoploss percentage, close the trade.
                if (trade.StopLossRate.HasValue && ticker.Bid < trade.StopLossRate.Value)
                    return SellType.TrailingStopLoss;

                // The new stop would be at a specific percentage above our starting point.
                var newStopRate = trade.OpenRate * (1 + (currentProfit - Global.Configuration.TradeOptions.TrailingStopPercentage));

                // Only update the trailing stop when its above our starting percentage and higher than the previous one.
                if (currentProfit > Global.Configuration.TradeOptions.TrailingStopStartingPercentage && (trade.StopLossRate < newStopRate || !trade.StopLossRate.HasValue))
                {
                    Global.Logger.Information("Trailing stop loss updated for {Market} from {StopLossRate} to {NewStopRate}", trade.Market, trade.StopLossRate?.ToString("0.00000000"), newStopRate.ToString("0.00000000"));

                    // The current profit percentage is high enough to create the trailing stop value.
                    // If we are getting our first stop loss raise, we set it to break even. From there the stop
                    // gets increased every given TrailingStopPercentage...
                    if (!trade.StopLossRate.HasValue)
                        trade.StopLossRate = trade.OpenRate;
                    else
                        trade.StopLossRate = Math.Round(newStopRate, 8);

                    return SellType.TrailingStopLossUpdated;
                }

                return SellType.None;
            }

            return SellType.None;
        }

        #endregion

        private static string GetOrderId()
        {
            return Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        private async Task SendNotification(string message)
        {
            //Global.Logger.Information(message);

            if (Runtime.NotificationManagers != null)
                foreach (var notificationManager in Runtime.NotificationManagers)
                    notificationManager.SendNotification(message);
        }

        private string TradeToString(Trade trade)
        {
            return string.Format($"#{trade.Market} with limit {trade.OpenRate:0.00000000} {Global.Configuration.TradeOptions.QuoteCurrency} " +
                                 $"({trade.Quantity:0.0000} {trade.GlobalSymbol.Replace(Global.Configuration.TradeOptions.QuoteCurrency, "")} " +
                                 $"{trade.OpenDate} " +
                                 $"({trade.TradeId})");
        }
    }
}
