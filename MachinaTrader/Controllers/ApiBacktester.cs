using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MachinaTrader.Globals;
using ExchangeSharp;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.TradeManagers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MachinaTrader.Backtester;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/backtester/")]
    public class ApiBacktester : Controller
    {
        [HttpGet]
        [Route("refresh")]
        public async Task<string> Refresh(string exchange, string coinsToBuy, string candleSize = "5")
        {
            BacktestOptions backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = new List<string>(new[] { coinsToBuy }),
                CandlePeriod = Int32.Parse(candleSize)
            };

            await DataRefresher.RefreshCandleData(x => Global.Logger.Information(x), backtestOptions, Global.DataStoreBacktest);

            return "Refresh Done";
        }

        [HttpGet]
        [Route("candlesAge")]
        public async Task<ActionResult> CandlesAge(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5")
        {
            List<string> coins = new List<string>();

            if (String.IsNullOrEmpty(coinsToBuy))
            {
                IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
                var exchangeCoins = api.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == baseCurrency);
                foreach (var coin in exchangeCoins)
                {
                    coins.Add(api.ExchangeSymbolToGlobalSymbol(coin.MarketName));
                }
            }
            else
            {
                Char delimiter = ',';
                String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
                foreach (var coin in coinsToBuyArray)
                {
                    coins.Add(coin.ToUpper());
                }
            }

            BacktestOptions backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };

            JObject result = new JObject
            {
                ["result"] = await DataRefresher.GetCacheAge(backtestOptions, Global.DataStoreBacktest)
            };
            return new JsonResult(result);
        }

        [HttpGet]
        [Route("refreshCandles")]
        public async Task<ActionResult> RefreshCandles(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5")
        {
            List<string> coins = new List<string>();

            if (String.IsNullOrEmpty(coinsToBuy))
            {
                IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
                var exchangeCoins = api.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == baseCurrency);
                foreach (var coin in exchangeCoins)
                {
                    coins.Add(api.ExchangeSymbolToGlobalSymbol(coin.MarketName));
                }
            }
            else
            {
                Char delimiter = ',';
                String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
                foreach (var coin in coinsToBuyArray)
                {
                    coins.Add(coin.ToUpper());
                }
            }

            BacktestOptions backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };

            await DataRefresher.RefreshCandleData(x => Global.Logger.Information(x), backtestOptions, Global.DataStoreBacktest);
            JObject result = new JObject
            {
                ["result"] = "success"
            };
            return new JsonResult(result);
        }

        [HttpGet]
        [Route("backtesterStrategy")]
        public ActionResult BacktesterStrategy()
        {

            JObject strategies = new JObject();
            foreach (var strategy in BacktestFunctions.GetTradingStrategies())
            {
                strategies[strategy.Name] = new JObject
                {
                    ["Name"] = strategy.Name,
                    ["ClassName"] = strategy.ToString().Replace("Mynt.Core.Strategies.", ""),
                    ["IdealPeriod"] = strategy.IdealPeriod.ToString(),
                    ["MinimumAmountOfCandles"] = strategy.MinimumAmountOfCandles.ToString()
                };
            }
            return new JsonResult(strategies);
        }

        [HttpGet]
        [Route("exchangePairs")]
        public ActionResult ExchangePairs(string exchange, string baseCurrency)
        {
            var result = new JArray();

            var symbolArray = new JArray();

            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
            var exchangeCoins = api.GetSymbolsMetadataAsync().Result;

            if (!String.IsNullOrEmpty(baseCurrency))
            {
                exchangeCoins = exchangeCoins.Where(e => e.BaseCurrency.ToLowerInvariant() == baseCurrency.ToLowerInvariant());
            }

            foreach (var coin in exchangeCoins)
            {
                symbolArray.Add(api.ExchangeSymbolToGlobalSymbol(coin.MarketName));
            }

            var baseCurrencyArray = new JArray();
            var exchangeBaseCurrencies = api.GetSymbolsMetadataAsync().Result.Select(m => m.BaseCurrency).Distinct();
            foreach (var currency in exchangeBaseCurrencies)
            {
                baseCurrencyArray.Add(currency);
            }

            result.Add(symbolArray);
            result.Add(baseCurrencyArray);

            return new JsonResult(result);
        }

        [HttpGet]
        [Route("backtesterResults")]
        public ActionResult BacktesterResults(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5", string strategy = "all")
        {
            JObject strategies = new JObject();

            List<string> coins = new List<string>();
            if (String.IsNullOrEmpty(coinsToBuy))
            {
                IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
                var exchangeCoins = api.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == baseCurrency);
                foreach (var coin in exchangeCoins)
                {
                    coins.Add(api.ExchangeSymbolToGlobalSymbol(coin.MarketName));
                }
            }
            else
            {
                Char delimiter = ',';
                String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
                foreach (var coin in coinsToBuyArray)
                {
                    coins.Add(coin.ToUpper());
                }
            }

            var backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };

            var cts = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            Parallel.ForEach(BacktestFunctions.GetTradingStrategies(), parallelOptions, async tradingStrategy =>
            {
                if (strategy != "all")
                {
                    var base64EncodedBytes = Convert.FromBase64String(strategy);
                    if (tradingStrategy.Name != Encoding.UTF8.GetString(base64EncodedBytes))
                    {
                        return;
                    }
                }
                var result = await BacktestFunctions.BackTestJson(tradingStrategy, backtestOptions, Global.DataStoreBacktest);
                foreach (var item in result)
                {
                    await Runtime.GlobalHubBacktest.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(item));
                }
            });

            return new JsonResult(strategies);
        }


        [HttpGet]
        [Route("getTickers")]
        public async Task<ActionResult> GetTickers(string exchange, string coinsToBuy, string strategy, string candleSize)
        {
            List<string> coins = new List<string>();
            Char delimiter = ',';
            String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
            foreach (var coin in coinsToBuyArray)
            {
                coins.Add(coin.ToUpper());
            }

            var backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                Coin = coinsToBuy,
                CandlePeriod = Int32.Parse(candleSize)
            };

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetCandles(backtestOptions, Global.DataStoreBacktest);

            return new JsonResult(items);
        }

        [HttpGet]
        [Route("getSignals")]
        public async Task<ActionResult> GetSignals(string exchange, string coinsToBuy, string strategy, string candleSize = "5")
        {
            var strategyName = WebUtility.HtmlDecode(strategy);

            List<string> coins = new List<string>();
            Char delimiter = ',';
            String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
            foreach (var coin in coinsToBuyArray)
            {
                coins.Add(coin.ToUpper());
            }

            var backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                Coin = coinsToBuy,
                CandlePeriod = Int32.Parse(candleSize)
            };

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetSignals(backtestOptions, Global.DataStoreBacktest, strategyName);

            return new JsonResult(items);
        }

        [HttpGet]
        [Route("simulation")]
        public async Task<bool> Simulation(string coinToBuy, string strategy, string fromDate, string toDate)
        {
            var candleProvider = new DatabaseCandleProvider();
            var globalFullApi = await Global.ExchangeApi.GetFullApi();
            await candleProvider.CacheAllData(globalFullApi, Global.Configuration.ExchangeOptions.FirstOrDefault().Exchange);

            var currentExchangeOption = Global.Configuration.ExchangeOptions.FirstOrDefault();

            var simulationStartingDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.ParseExact(fromDate, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
            var simulationEndingDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.ParseExact(toDate, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));

            var tradeManager = new TradeManager();

            currentExchangeOption.SimulationCurrentDate = simulationStartingDate;

            while (currentExchangeOption.SimulationCurrentDate <= simulationEndingDate)
            {
                Global.Logger.Information($"------ SimulationCurrentDate start: {currentExchangeOption.SimulationCurrentDate}");
                var watch1 = System.Diagnostics.Stopwatch.StartNew();

                await tradeManager.LookForNewTrades(strategy);
                await tradeManager.UpdateExistingTrades();

                currentExchangeOption.SimulationCurrentDate = currentExchangeOption.SimulationCurrentDate.AddMinutes(5);

                watch1.Stop();
                Global.Logger.Information($"------SimulationCurrentDate end: {currentExchangeOption.SimulationCurrentDate} in #{watch1.Elapsed.TotalSeconds} seconds");
            }

            return true;
        }

    }

}
