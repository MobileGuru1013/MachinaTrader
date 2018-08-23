using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LiteDB;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Data.LiteDB
{

    public class LiteDbDataStoreBacktest : IDataStoreBacktest
    {
        private LiteDatabase _database;

        public LiteDbDataStoreBacktest(LiteDbOptions options)
        {
            _database = new LiteDatabase(options.LiteDbName);
            GetDatabase(new BacktestOptions());
        }

        public static string GetDatabase(BacktestOptions backtestOptions)
        {
            if (!Directory.Exists(backtestOptions.DataFolder))
                Directory.CreateDirectory(backtestOptions.DataFolder);

            if (backtestOptions.Coin == null)
            {
                return backtestOptions.DataFolder.Replace("\\", "/");
            }
            return backtestOptions.DataFolder.Replace("\\", "/") + "/" + backtestOptions.Exchange + "_" + backtestOptions.Coin + ".db";
        }

        private static readonly ConcurrentDictionary<string, DataStoreBacktest> DatabaseInstances = new ConcurrentDictionary<string, DataStoreBacktest>();

        private class DataStoreBacktest
        {
            private readonly LiteDatabase _liteDatabase;

            private DataStoreBacktest(string databasePath)
            {
                // Workaround on OSX -> Dont support Locking/unlocking file regions 
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _liteDatabase = new LiteDatabase("filename=" + databasePath + ";mode=Exclusive;utc=true");
                }
                else
                {
                    _liteDatabase = new LiteDatabase("filename=" + databasePath + ";mode=Exclusive;mode=Shared;utc=true");
                }
            }

            public static DataStoreBacktest GetInstance(string databasePath)
            {
                if (!DatabaseInstances.ContainsKey(databasePath))
                {
                    DatabaseInstances[databasePath] = new DataStoreBacktest(databasePath);
                }
                return DatabaseInstances[databasePath];
            }

            public LiteCollection<T> GetTable<T>(string collectionName = null) where T : new()
            {
                if (collectionName == null)
                {
                    return _liteDatabase.GetCollection<T>(typeof(T).Name);
                }
                return _liteDatabase.GetCollection<T>(collectionName);
            }
        }

        public async Task InitializeAsync()
        {
        }

        public async Task<List<Candle>> GetBacktestCandlesBetweenTime(BacktestOptions backtestOptions)
        {
            try
            {
                LiteCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(GetDatabase(backtestOptions)).GetTable<CandleAdapter>("Candle_" + backtestOptions.CandlePeriod);
                candleCollection.EnsureIndex("Timestamp");
                List<CandleAdapter> candles = candleCollection.Find(Query.Between("Timestamp", backtestOptions.StartDate, backtestOptions.EndDate), Query.Ascending).ToList();
                var items = Mapping.Mapper.Map<List<Candle>>(candles);
                return items;
            }
            catch (Exception ex)
            {
                Global.Logger.Error(ex.ToString());
                throw;
            }

        }

        public async Task<Candle> GetBacktestFirstCandle(BacktestOptions backtestOptions)
        {
            LiteCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(GetDatabase(backtestOptions)).GetTable<CandleAdapter>("Candle_" + backtestOptions.CandlePeriod);
            candleCollection.EnsureIndex("Timestamp");
            CandleAdapter lastCandle = candleCollection.Find(Query.All("Timestamp"), limit: 1).FirstOrDefault();
            var items = Mapping.Mapper.Map<Candle>(lastCandle);
            return items;
        }

        public async Task<Candle> GetBacktestLastCandle(BacktestOptions backtestOptions)
        {
            LiteCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(GetDatabase(backtestOptions)).GetTable<CandleAdapter>("Candle_" + backtestOptions.CandlePeriod);
            candleCollection.EnsureIndex("Timestamp");
            CandleAdapter lastCandle = candleCollection.Find(Query.All("Timestamp", Query.Descending), limit: 1).FirstOrDefault();
            var items = Mapping.Mapper.Map<Candle>(lastCandle);
            return items;
        }

        public async Task SaveBacktestCandlesBulk(List<Candle> candles, BacktestOptions backtestOptions)
        {
            var items = Mapping.Mapper.Map<List<CandleAdapter>>(candles);
            LiteCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(GetDatabase(backtestOptions)).GetTable<CandleAdapter>("Candle_" + backtestOptions.CandlePeriod);
            candleCollection.EnsureIndex("Timestamp");
            candleCollection.InsertBulk(items);
        }

        public async Task SaveBacktestCandlesBulkCheckExisting(List<Candle> candles, BacktestOptions backtestOptions)
        {
            var items = Mapping.Mapper.Map<List<CandleAdapter>>(candles);
            LiteCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(GetDatabase(backtestOptions)).GetTable<CandleAdapter>("Candle_" + backtestOptions.CandlePeriod); foreach (var item in items)
            {
                var checkData = candleCollection.FindOne(x => x.Timestamp == item.Timestamp);
                if (checkData == null)
                {
                    candleCollection.Insert(item);
                }
            }
        }

        public async Task SaveBacktestCandle(Candle candle, BacktestOptions backtestOptions)
        {
            var item = Mapping.Mapper.Map<CandleAdapter>(candle);
            LiteCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(GetDatabase(backtestOptions)).GetTable<CandleAdapter>("Candle_" + backtestOptions.CandlePeriod);
            candleCollection.EnsureIndex("Timestamp");
            var newCandle = candleCollection.FindOne(x => x.Timestamp == item.Timestamp);
            if (newCandle == null)
            {
                candleCollection.Insert(item);
            }
        }

        public async Task<List<string>> GetBacktestAllDatabases(BacktestOptions backtestOptions)
        {
            List<string> allDatabaseFiles = Directory.GetFiles(backtestOptions.DataFolder, "*.db", SearchOption.TopDirectoryOnly).ToList();
            return allDatabaseFiles;
        }

        public async Task DeleteBacktestDatabase(BacktestOptions backtestOptions)
        {
            if (File.Exists(GetDatabase(backtestOptions)))
            {
                File.Delete(GetDatabase(backtestOptions));
            }
        }

        public async Task SaveBacktestTradeSignalsBulk(List<TradeSignal> signals, BacktestOptions backtestOptions)
        {
            var items = Mapping.Mapper.Map<List<TradeSignalAdapter>>(signals);

            LiteCollection<TradeSignalAdapter> itemCollection = DataStoreBacktest.GetInstance(GetDatabase(backtestOptions)).GetTable<TradeSignalAdapter>("Signals_" + backtestOptions.CandlePeriod);

            foreach (var item in items)
            {
                itemCollection.Delete(i => i.StrategyName == item.StrategyName);
            }

            // TradeSignalAdapter lastCandle = itemCollection.Find(Query.All("Timestamp", Query.Descending), limit: 1).FirstOrDefault();

            itemCollection.EnsureIndex("Timestamp");
            itemCollection.InsertBulk(items);
        }

        public async Task<List<TradeSignal>> GetBacktestSignalsByStrategy(BacktestOptions backtestOptions, string strategy)
        {
            var itemCollection = DataStoreBacktest.GetInstance(GetDatabase(backtestOptions)).GetTable<TradeSignalAdapter>("Signals_" + backtestOptions.CandlePeriod);
            itemCollection.EnsureIndex("StrategyName");
            var items = itemCollection.Find(Query.Where("StrategyName", s => s.AsString == strategy), Query.Descending).ToList();
            var result = Mapping.Mapper.Map<List<TradeSignal>>(items);
            return result;
        }
    }
}
