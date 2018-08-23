using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MachinaTrader.Globals.Helpers;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Extensions;

namespace MachinaTrader.Backtester
{
    public class BackTestRunner
    {
        public async Task<List<BackTestResult>> RunSingleStrategy(ITradingStrategy strategy, BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            var results = new List<BackTestResult>();

            // Go through our coinpairs and backtest them.
            foreach (string globalSymbol in backtestOptions.Coins)
            {
                var candleProvider = new DatabaseCandleProvider();
                backtestOptions.Coin = globalSymbol;

                // This creates a list of buy signals.
                var candles = await candleProvider.GetCandles(backtestOptions, dataStore);
                var backTestResult = new BackTestResult { Market = globalSymbol };

                try
                {
                    var trend = strategy.Prepare(candles);
                    var signals = new List<TradeSignal>();

                    for (int i = 0; i < trend.Count; i++)
                    {
                        if (trend[i] == TradeAdvice.Buy)
                        {
                            var id = Guid.NewGuid();

                            signals.Add(new TradeSignal
                            {
                                Id = id,
                                MarketName = globalSymbol,
                                Price = candles[i].Close,
                                TradeAdvice = TradeAdvice.Buy,
                                SignalCandle = candles[i],
                                Timestamp = candles[i].Timestamp,
                                StrategyName = strategy.Name
                            });

                            // Calculate win/lose forwards from buy point
                            for (int j = i; j < trend.Count; j++)
                            {
                                // Sell as soon as the strategy tells us to..
                                if (trend[j] == TradeAdvice.Sell
                                    || ShouldSell((double)candles[i].Close, (double)candles[j].Close, candles[j].Timestamp) != SellType.None
                                    )
                                {
                                    //if (candles[i].Close == 0 || candles[j].Close == 0)
                                    //    continue;

                                    // We ignore fees for now. Goal of the backtester is to compare strategy efficiency.
                                    var currentProfitPercentage = ((candles[j].Close - candles[i].Close) / candles[i].Close) * 100;
                                    var quantity = backtestOptions.StakeAmount / candles[i].Close; // We always trade with 0.1 BTC.
                                    var currentProfit = (candles[j].Close - candles[i].Close) * quantity;

                                    backTestResult.Trades.Add(new BackTestTradeResult
                                    {
                                        Market = globalSymbol,
                                        Quantity = quantity,
                                        OpenRate = candles[i].Close,
                                        CloseRate = candles[j].Close,
                                        ProfitPercentage = currentProfitPercentage,
                                        Profit = currentProfit,
                                        Duration = j - i,
                                        StartDate = candles[i].Timestamp,
                                        EndDate = candles[j].Timestamp
                                    });

                                    signals.Add(new TradeSignal
                                    {
                                        Id = Guid.NewGuid(),
                                        ParentId = id,
                                        MarketName = globalSymbol,
                                        Price = candles[j].Close,
                                        TradeAdvice = TradeAdvice.Sell,
                                        SignalCandle = candles[j],
                                        Profit = currentProfit,
                                        PercentageProfit = currentProfitPercentage,
                                        Timestamp = candles[j].Timestamp,
                                        StrategyName = strategy.Name
                                    });

                                    if (backtestOptions.OnlyStartNewTradesWhenSold)
                                        i = j;

                                    break;
                                }
                            }
                        }
                    }

                    await candleProvider.SaveTradeSignals(backtestOptions, dataStore, signals);
                }
                catch (Exception ex)
                {
                    ConsoleUtility.WriteColoredLine($"Error in Strategy: {strategy.Name}", ConsoleColor.Red);
                    ConsoleUtility.WriteColoredLine($"\t{ex.Message}", ConsoleColor.Red);
                }

                results.Add(backTestResult);
            }

            return results;
        }

        private SellType ShouldSell(double tradeOpenRate, double currentRateBid, DateTime utcNow)
        {
            var currentProfit = (currentRateBid - tradeOpenRate) / tradeOpenRate;

            if (currentProfit < -0.07) //stopLossPercentage
                return SellType.StopLoss;

            //if (currentProfit >= 0.08)
            //    return SellType.Immediate;

            //if (currentProfit > 0.04)
            //	return SellType.Timed;

            // Check if time matches and current rate is above threshold
            //foreach (var item in returnOnInvestment)
            //{
            //	var timeDiff = (utcNow - tradeOpenRate).TotalSeconds / 60;

            //	if (timeDiff >= item.Duration && currentProfit > item.Profit)
            //		return SellType.Timed;
            //}

            return SellType.None;
        }
    }
}
