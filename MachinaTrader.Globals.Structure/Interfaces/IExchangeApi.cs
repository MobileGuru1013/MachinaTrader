using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExchangeSharp;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Globals.Structure.Interfaces
{
    public interface IExchangeApi
    {
        Task<string> Buy(string market, decimal quantity, decimal rate);

        Task<string> Sell(string market, decimal quantity, decimal rate);

        Task<AccountBalance> GetBalance(string currency);

        Task<List<Models.MarketSummary>> GetMarketSummaries(string quoteCurrency);

        Task<Order> GetOrder(string orderId, string market);

        Task<List<OpenOrder>> GetOpenOrders(string market);

        Task CancelOrder(string orderId, string market);

        Task<Ticker> GetTicker(string market);

        Task<OrderBook> GetOrderBook(string market);

        Task<List<Candle>> GetTickerHistory(string market, Period period, DateTime startDate, DateTime? endDate = null);

        Task<List<Candle>> GetTickerHistory(string market, Period period, int length);

        Task<string> GlobalSymbolToExchangeSymbol(string symbol);

        Task<string> ExchangeCurrencyToGlobalCurrency(string symbol);

        Task<ExchangeAPI> GetFullApi();

    }
}
