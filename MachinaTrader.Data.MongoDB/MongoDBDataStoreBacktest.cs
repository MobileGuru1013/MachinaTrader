using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using MongoDB.Driver;

namespace MachinaTrader.Data.MongoDB
{
    public class MongoDbDataStoreBacktest : IDataStoreBacktest
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        public static MongoDbOptions MongoDbOptions;
        public string MongoDbBaseName;

        public MongoDbDataStoreBacktest(MongoDbOptions options)
        {
            MongoDbOptions = options;
            _client = new MongoClient(options.MongoUrl);
            _database = _client.GetDatabase(options.MongoDatabaseName);
            MongoDbBaseName = "Backtest_Candle_";
        }

        public static string GetDatabase(BacktestOptions backtestOptions)
        {
            return backtestOptions.Exchange + "_" + backtestOptions.Coin + "_" + backtestOptions.CandlePeriod;
        }

        public class DataStoreBacktest
        {
            private MongoClient _client;
            private IMongoDatabase _database;
            private static Dictionary<string, DataStoreBacktest> _instance = new Dictionary<string, DataStoreBacktest>();

            private DataStoreBacktest(string databaseName)
            {
                _client = new MongoClient(MongoDbOptions.MongoUrl);
                _database = _client.GetDatabase(databaseName);
            }

            public static DataStoreBacktest GetInstance(string databaseName)
            {
                if (!_instance.ContainsKey(databaseName))
                {
                    _instance["databaseName"] = new DataStoreBacktest(databaseName);
                }
                return _instance["databaseName"];
            }

            public IMongoCollection<T> GetTable<T>(string collectionName = null) where T : new()
            {
                if (collectionName == null)
                {
                    return _database.GetCollection<T>(typeof(T).Name);
                }
                return _database.GetCollection<T>(collectionName);
            }
        }


        public async Task InitializeAsync()
        {
        }

        public async Task<List<Candle>> GetBacktestCandlesBetweenTime(BacktestOptions backtestOptions)
        {
            try
            {
                IMongoCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(MongoDbBaseName + backtestOptions.CandlePeriod).GetTable<CandleAdapter>(backtestOptions.Exchange + "_" + backtestOptions.Coin);
                List<CandleAdapter> candles = await candleCollection.Find(entry => entry.Timestamp >= backtestOptions.StartDate && entry.Timestamp <= backtestOptions.EndDate).ToListAsync();

#warning TODO: Sort operation used more than the maximum 33554432 bytes of RAM. Add an index, or specify a smaller limit=> temporarly order by LINQ

                var items = Mapping.Mapper.Map<List<Candle>>(candles).OrderBy(c=>c.Timestamp).ToList();
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
            IMongoCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(MongoDbBaseName + backtestOptions.CandlePeriod).GetTable<CandleAdapter>(backtestOptions.Exchange + "_" + backtestOptions.Coin);
            CandleAdapter lastCandle = await candleCollection.Find(_ => true).SortBy(e => e.Timestamp).Limit(1).FirstOrDefaultAsync();
            var items = Mapping.Mapper.Map<Candle>(lastCandle);
            return items;
        }

        public async Task<Candle> GetBacktestLastCandle(BacktestOptions backtestOptions)
        {
            IMongoCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(MongoDbBaseName + backtestOptions.CandlePeriod).GetTable<CandleAdapter>(backtestOptions.Exchange + "_" + backtestOptions.Coin);
            CandleAdapter lastCandle = await candleCollection.Find(_ => true).SortByDescending(e => e.Timestamp).Limit(1).FirstOrDefaultAsync();
            var items = Mapping.Mapper.Map<Candle>(lastCandle);
            return items;
        }

        public async Task SaveBacktestCandlesBulk(List<Candle> candles, BacktestOptions backtestOptions)
        {
            var items = Mapping.Mapper.Map<List<CandleAdapter>>(candles);
            IMongoCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(MongoDbBaseName + backtestOptions.CandlePeriod).GetTable<CandleAdapter>(backtestOptions.Exchange + "_" + backtestOptions.Coin);
            await candleCollection.InsertManyAsync(items);
        }

        public async Task SaveBacktestCandlesBulkCheckExisting(List<Candle> candles, BacktestOptions backtestOptions)
        {
            var items = Mapping.Mapper.Map<List<CandleAdapter>>(candles);
            IMongoCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(MongoDbBaseName + backtestOptions.CandlePeriod).GetTable<CandleAdapter>(backtestOptions.Exchange + "_" + backtestOptions.Coin);
            FindOptions<CandleAdapter> marketCandleFindOptions = new FindOptions<CandleAdapter> { Limit = 1 };
            foreach (var item in items)
            {
                IAsyncCursor<CandleAdapter> checkData = await candleCollection.FindAsync(x => x.Timestamp.Equals(item.Timestamp), marketCandleFindOptions);
                if (await checkData.FirstOrDefaultAsync() == null)
                {
                    await candleCollection.InsertOneAsync(item);
                }
            }
        }

        public async Task SaveBacktestCandle(Candle candle, BacktestOptions backtestOptions)
        {
            var item = Mapping.Mapper.Map<CandleAdapter>(candle);
            IMongoCollection<CandleAdapter> candleCollection = DataStoreBacktest.GetInstance(MongoDbBaseName + backtestOptions.CandlePeriod).GetTable<CandleAdapter>(backtestOptions.Exchange + "_" + backtestOptions.Coin);
            FindOptions<CandleAdapter> marketCandleFindOptions = new FindOptions<CandleAdapter> { Limit = 1 };
            IAsyncCursor<CandleAdapter> checkData = await candleCollection.FindAsync(x => x.Timestamp == item.Timestamp, marketCandleFindOptions);
            if (await checkData.FirstOrDefaultAsync() == null)
            {
                await candleCollection.InsertOneAsync(item);
            }
        }

        public async Task<List<string>> GetBacktestAllDatabases(BacktestOptions backtestOptions)
        {
            List<string> allDatabases = new List<string>();
            var dbList = await _client.GetDatabase(MongoDbOptions.MongoDatabaseName).ListCollectionsAsync();
            foreach (var item in await dbList.ToListAsync())
            {
                allDatabases.Add(item.ToString());
            }
            return allDatabases;
        }

        public async Task DeleteBacktestDatabase(BacktestOptions backtestOptions)
        {
            var dbList = _client.GetDatabase(MongoDbOptions.MongoDatabaseName);
            dbList.DropCollection(backtestOptions.Exchange + "_" + backtestOptions.Coin);
        }


        public async Task SaveBacktestTradeSignalsBulk(List<TradeSignal> signals, BacktestOptions backtestOptions)
        {
            var items = Mapping.Mapper.Map<List<TradeSignalAdapter>>(signals);

            IMongoCollection<TradeSignalAdapter> itemCollection = DataStoreBacktest.GetInstance("Signals_" + MongoDbBaseName + backtestOptions.CandlePeriod).GetTable<TradeSignalAdapter>(backtestOptions.Exchange + "_" + backtestOptions.Coin);

            foreach (var item in items)
            {
                await itemCollection.DeleteManyAsync(i => i.StrategyName == item.StrategyName);
            }

            await itemCollection.InsertManyAsync(items);
        }

        public async Task<List<TradeSignal>> GetBacktestSignalsByStrategy(BacktestOptions backtestOptions, string strategy)
        {
            IMongoCollection<TradeSignalAdapter> itemCollection = DataStoreBacktest.GetInstance("Signals_" + MongoDbBaseName + backtestOptions.CandlePeriod).GetTable<TradeSignalAdapter>(backtestOptions.Exchange + "_" + backtestOptions.Coin);
            var items = await itemCollection.Find(entry => entry.StrategyName == strategy).ToListAsync();

#warning TODO: Sort operation used more than the maximum 33554432 bytes of RAM. Add an index, or specify a smaller limit=> temporarly order by LINQ

            var result = Mapping.Mapper.Map<List<TradeSignal>>(items).OrderBy(c => c.Timestamp).ToList();
            return result;
        }

    }
}
