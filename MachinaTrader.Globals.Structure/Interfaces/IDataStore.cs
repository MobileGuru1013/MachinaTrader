using System.Collections.Generic;
using System.Threading.Tasks;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Globals.Structure.Interfaces
{
    public interface IDataStore
    {
        // Initialization
        Task InitializeAsync();
 
        // Trade/order related methods
        Task<List<Trade>> GetActiveTradesAsync();
        Task SaveTradesAsync(List<Trade> trades);
        Task SaveTradeAsync(Trade trade);
        Task<List<Trade>> GetClosedTradesAsync();

        Task SaveWalletTransactionAsync(WalletTransaction walletTransaction);
        Task<List<WalletTransaction>> GetWalletTransactionsAsync();

        // Trader related methods
        Task<List<Trader>> GetTradersAsync();
        Task<List<Trader>> GetBusyTradersAsync();
        Task<List<Trader>> GetAvailableTradersAsync();
        Task SaveTradersAsync(List<Trader> traders);
        Task SaveTraderAsync(Trader trader);
    }
}
