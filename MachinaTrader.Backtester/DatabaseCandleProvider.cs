using MachinaTrader.Globals.Structure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExchangeSharp;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace MachinaTrader.Backtester
{
    public class DatabaseCandleProvider
    {
        public async Task<List<Candle>> GetCandles(BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            if (backtestOptions.EndDate == DateTime.MinValue)
            {
                backtestOptions.EndDate = DateTime.UtcNow;
            }

            List<Candle> candles = await dataStore.GetBacktestCandlesBetweenTime(backtestOptions);

            return candles;
        }

        public async Task SaveTradeSignals(BacktestOptions backtestOptions, IDataStoreBacktest dataStore, List<TradeSignal> signals)
        {
            if (backtestOptions.EndDate == DateTime.MinValue)
            {
                backtestOptions.EndDate = DateTime.UtcNow;
            }

            await dataStore.SaveBacktestTradeSignalsBulk(signals, backtestOptions);
        }

        public async Task<List<TradeSignal>> GetSignals(BacktestOptions backtestOptions, IDataStoreBacktest dataStore, string strategy)
        {
            if (backtestOptions.EndDate == DateTime.MinValue)
            {
                backtestOptions.EndDate = DateTime.UtcNow;
            }

            List<TradeSignal> items = await dataStore.GetBacktestSignalsByStrategy(backtestOptions, strategy);

            return items;
        }

        public async Task CacheAllData(ExchangeAPI api, Exchange exchange)
        {
            Global.Logger.Information($"Starting CacheAllData");
            var watch1 = System.Diagnostics.Stopwatch.StartNew();

            var exchangeCoins = api.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == Global.Configuration.TradeOptions.QuoteCurrency);

            // If there are items on the only trade list remove the rest
            if (Global.Configuration.TradeOptions.OnlyTradeList.Count > 0)
                exchangeCoins = exchangeCoins.Where(m => Global.Configuration.TradeOptions.OnlyTradeList.Any(c => c.Contains(m.MarketName))).ToList();

            var currentExchangeOption = Global.Configuration.ExchangeOptions.FirstOrDefault();

            IExchangeAPI realExchange = ExchangeAPI.GetExchangeAPI(api.Name);

            foreach (var coin in exchangeCoins)
            {
                var symbol = coin.MarketName;

                if (realExchange is ExchangeBinanceAPI)
                    symbol = api.ExchangeSymbolToGlobalSymbol(symbol);

                var backtestOptions = new BacktestOptions
                {
                    DataFolder = Global.DataPath,
                    Exchange = exchange,
                    Coin = symbol,
                    CandlePeriod = Int32.Parse(currentExchangeOption.SimulationCandleSize)
                };

                var key1 = api.Name + backtestOptions.Coin + backtestOptions.CandlePeriod;
                if (Global.AppCache.Get<List<Candle>>(key1) != null)
                    continue;

                Candle databaseFirstCandle = Global.DataStoreBacktest.GetBacktestFirstCandle(backtestOptions).Result;
                Candle databaseLastCandle = Global.DataStoreBacktest.GetBacktestLastCandle(backtestOptions).Result;

                if (databaseFirstCandle == null || databaseLastCandle == null)
                    continue;

                backtestOptions.StartDate = databaseFirstCandle.Timestamp;
                backtestOptions.EndDate = databaseLastCandle.Timestamp;

                var candleProvider = new DatabaseCandleProvider();
                var _candle15 = candleProvider.GetCandles(backtestOptions, Global.DataStoreBacktest).Result;

                Global.AppCache.Remove(backtestOptions.Coin + backtestOptions.CandlePeriod);
                Global.AppCache.Add(api.Name + backtestOptions.Coin + backtestOptions.CandlePeriod, _candle15, new MemoryCacheEntryOptions());

                Global.Logger.Information($"   Cached {key1}");

                backtestOptions.CandlePeriod = 1;

                var key2 = api.Name + backtestOptions.Coin + backtestOptions.CandlePeriod;
                if (Global.AppCache.Get<List<Candle>>(key2) != null)
                    continue;

                Candle database1FirstCandle = Global.DataStoreBacktest.GetBacktestFirstCandle(backtestOptions).Result;
                Candle database1LastCandle = Global.DataStoreBacktest.GetBacktestLastCandle(backtestOptions).Result;

                if (database1FirstCandle == null || database1LastCandle == null)
                    continue;

                backtestOptions.StartDate = database1FirstCandle.Timestamp;
                backtestOptions.EndDate = database1LastCandle.Timestamp;

                var _candle1 = candleProvider.GetCandles(backtestOptions, Global.DataStoreBacktest).Result;

                Global.AppCache.Remove(backtestOptions.Coin + backtestOptions.CandlePeriod);
                Global.AppCache.Add(api.Name + backtestOptions.Coin + backtestOptions.CandlePeriod, _candle1, new MemoryCacheEntryOptions());

                Global.Logger.Information($"   Cached {key2}");
            }

            watch1.Stop();
            Global.Logger.Warning($"Ended CacheAllData in #{watch1.Elapsed.TotalSeconds} seconds");
        }
    }
}
