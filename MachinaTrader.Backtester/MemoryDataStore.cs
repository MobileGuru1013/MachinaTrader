using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Backtester
{
    public class MemoryDataStore : IDataStore
    {
        private List<Trade> _trades = new List<Trade>();
        private List<WalletTransaction> _walletTransactions = new List<WalletTransaction>();

        public MemoryDataStore()
        {
        }

        public async Task InitializeAsync()
        {
        }

        public async Task<List<Trade>> GetClosedTradesAsync()
        {
            var items = _trades.Where(x => !x.IsOpen).ToList();
            return items;
        }

        public async Task<List<Trade>> GetActiveTradesAsync()
        {
            var items = _trades.Where(x => x.IsOpen).ToList();
            return items;
        }

        public async Task<List<Trader>> GetAvailableTradersAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<Trader>> GetBusyTradersAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SaveTradeAsync(Trade trade)
        {
            var item = _trades.Where(x => x.TradeId.Equals(trade.TradeId)).FirstOrDefault();

            if (item != null)
            {
                _trades.Remove(item);
            }
           
            _trades.Add(trade);
        }

        public async Task SaveWalletTransactionAsync(WalletTransaction walletTransaction)
        {
            var item = _walletTransactions.Where(x => x.Id.Equals(walletTransaction.Id)).FirstOrDefault();

            if (item != null)
            {
                _walletTransactions.Remove(item);
            }

            walletTransaction.Id = Guid.NewGuid();
            _walletTransactions.Add(walletTransaction);
        }

        public async Task<List<WalletTransaction>> GetWalletTransactionsAsync()
        {
            var items = _walletTransactions.OrderBy(s => s.Date).ToList();
            return items;
        }

        public async Task SaveTraderAsync(Trader trader)
        {
            throw new NotImplementedException();
        }

        public async Task SaveTradersAsync(List<Trader> traders)
        {
            throw new NotImplementedException();
        }

        public async Task SaveTradesAsync(List<Trade> trades)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Trader>> GetTradersAsync()
        {
            throw new NotImplementedException();
        }
    }
}
