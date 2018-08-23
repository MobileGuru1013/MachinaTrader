using System.Collections.Generic;
using System.Threading.Tasks;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using MongoDB.Driver;

namespace MachinaTrader.Data.MongoDB
{
    public class MongoDbDataStore : IDataStore
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        public static MongoDbOptions MongoDbOptions;
        private IMongoCollection<TraderAdapter> _traderAdapter;
        private IMongoCollection<TradeAdapter> _ordersAdapter;
        private IMongoCollection<WalletTransactionAdapter> _walletTransactionsAdapter;

        public MongoDbDataStore(MongoDbOptions options)
        {
            MongoDbOptions = options;
            _client = new MongoClient(options.MongoUrl);
            _database = _client.GetDatabase("MachinaTrader");
            _ordersAdapter = _database.GetCollection<TradeAdapter>("Orders");
            _traderAdapter = _database.GetCollection<TraderAdapter>("Traders");
            _walletTransactionsAdapter = _database.GetCollection<WalletTransactionAdapter>("WalletTransactions");
        }

        public async Task InitializeAsync()
        {
        }

        public async Task<List<Trade>> GetClosedTradesAsync()
        {
            var trades = await _ordersAdapter.Find(x => !x.IsOpen).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trade>>(trades);

            return items;
        }

        public async Task<List<Trade>> GetActiveTradesAsync()
        {
            var trades = await _ordersAdapter.Find(x => x.IsOpen).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trade>>(trades);

            return items;
        }

        public async Task<List<Trader>> GetAvailableTradersAsync()
        {
            var traders = await _traderAdapter.Find(x => !x.IsBusy && !x.IsArchived).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trader>>(traders);

            return items;
        }

        public async Task<List<Trader>> GetBusyTradersAsync()
        {
            var traders = await _traderAdapter.Find(x => x.IsBusy && !x.IsArchived).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trader>>(traders);

            return items;
        }

        public async Task SaveTradeAsync(Trade trade)
        {
            var item = Mapping.Mapper.Map<TradeAdapter>(trade);
            TradeAdapter checkExist = await _ordersAdapter.Find(x => x.TradeId.Equals(item.TradeId)).FirstOrDefaultAsync();
            if (checkExist != null)
            {
                await _ordersAdapter.ReplaceOneAsync(x => x.TradeId.Equals(item.TradeId), item);
            } else
            {
                await _ordersAdapter.InsertOneAsync(item);
            }
        }

        public async Task SaveWalletTransactionAsync(WalletTransaction walletTransaction)
        {
            var item = Mapping.Mapper.Map<WalletTransactionAdapter>(walletTransaction);
            WalletTransactionAdapter checkExist = await _walletTransactionsAdapter.Find(x => x.Id.Equals(item.Id)).FirstOrDefaultAsync();
            if (checkExist != null)
            {
                await _walletTransactionsAdapter.ReplaceOneAsync(x => x.Id.Equals(item.Id), item);
            }
            else
            {
                await _walletTransactionsAdapter.InsertOneAsync(item);
            }
        }

        public async Task<List<WalletTransaction>> GetWalletTransactionsAsync()
        {
            var walletTransactions = await _walletTransactionsAdapter.Find(FilterDefinition<WalletTransactionAdapter>.Empty).SortBy(s=>s.Date).ToListAsync();
            var items = Mapping.Mapper.Map<List<WalletTransaction>>(walletTransactions);

            return items;
        }

        public async Task SaveTraderAsync(Trader trader)
        {
            var item = Mapping.Mapper.Map<TraderAdapter>(trader);
            TraderAdapter checkExist = await _traderAdapter.Find(x => x.Identifier.Equals(item.Identifier)).FirstOrDefaultAsync();
            if (checkExist != null)
            {
                await _traderAdapter.ReplaceOneAsync(x => x.Identifier.Equals(item.Identifier), item);
            }
            else
            {
                await _traderAdapter.InsertOneAsync(item);
            }
        }

        public async Task SaveTradersAsync(List<Trader> traders)
        {
            var items = Mapping.Mapper.Map<List<TraderAdapter>>(traders);

            foreach (var item in items)
            {
                TraderAdapter checkExist = await _traderAdapter.Find(x => x.Identifier.Equals(item.Identifier)).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    await _traderAdapter.ReplaceOneAsync(x => x.Identifier.Equals(item.Identifier), item);
                }
                else
                {
                    await _traderAdapter.InsertOneAsync(item);
                }
            }
        }

        public async Task SaveTradesAsync(List<Trade> trades)
        {
            var items = Mapping.Mapper.Map<List<TradeAdapter>>(trades);

            foreach (var item in items)
            {
                TradeAdapter checkExist = await _ordersAdapter.Find(x => x.TradeId.Equals(item.TradeId)).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    await _ordersAdapter.ReplaceOneAsync(x => x.TradeId.Equals(item.TradeId), item);
                }
                else
                {
                    await _ordersAdapter.InsertOneAsync(item);
                }
            }
        }

        public async Task<List<Trader>> GetTradersAsync()
        {
            var traders = await _traderAdapter.Find(_ => true).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trader>>(traders);

            return items;
        }

    }
}
