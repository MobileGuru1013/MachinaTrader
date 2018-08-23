using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MachinaTrader.Models;
using ExchangeSharp;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Controllers
{

    [Authorize, Route("api/trading/")]
    public class ApiTrading : Controller
    {
        [HttpGet]
        [Route("exchangePairsExchangeSymbols")]
        public async Task<ActionResult> ExchangePairsExchangeSymbols(string exchange)
        {
            JArray symbolArray = new JArray();
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
            var exchangeCoins = await api.GetSymbolsAsync();
            foreach (var coin in exchangeCoins)
            {
                symbolArray.Add(coin);
            }
            return new JsonResult(symbolArray);
        }

        [HttpGet]
        [Route("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var fullApi = Global.ExchangeApi.GetFullApi().Result;
            var balance = await fullApi.GetAmountsAvailableToTradeAsync();
            return new JsonResult(balance);
        }

        [HttpGet]
        [Route("history")]
        public async Task<IActionResult> GetHistory()
        {
            var fullApi = Global.ExchangeApi.GetFullApi().Result;
            var balance = await fullApi.GetCompletedOrderDetailsAsync("ETHBTC");
            return new JsonResult(balance);
        }

        [HttpGet]
        [Route("getTicker")]
        public async Task<IActionResult> GetTicker(string symbol)
        {
            var ticker = await Global.ExchangeApi.GetTicker(symbol);
            return new JsonResult(ticker);
        }

        [HttpGet]
        [Route("topVolumeCurrencies")]
        public async Task<IActionResult> GetTopVoumeCurrencies(int limit = 20)
        {
            var fullApi = await Global.ExchangeApi.GetFullApi();
            var getCurrencies = fullApi.GetTickers();
            var objListOrder = getCurrencies
                .OrderByDescending(o => o.Value.Volume.ConvertedVolume)
                .ToList();

            JArray topCurrencies = new JArray();
            int count = 0;
            foreach (var currency in objListOrder)
            {
                topCurrencies.Add(currency.Key);
                count = count + 1;
                if (count > limit)
                {
                    break;
                }
            }
            return new JsonResult(topCurrencies);
        }

        [HttpGet]
        [Route("globalToExchangeSymbol")]
        public string GlobalToExchangeSymbol(string exchange, string symbol)
        {
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
            string exchangeSymbol = api.GlobalSymbolToExchangeSymbol(symbol);
            return exchangeSymbol;
        }

        [HttpGet]
        [Route("globalToTradingViewSymbol")]
        public string GlobalToTradingViewSymbol(string exchange, string symbol)
        {
            //Trading view use same format as Binance -> BTC-ETH is ETHBTC
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI("binance");
            string exchangeSymbol = api.GlobalSymbolToExchangeSymbol(symbol);
            return exchangeSymbol;
        }

        [HttpGet]
        [Route("trade/{tradeId}")]
        public async Task<IActionResult> TradingTrade(string tradeId)
        {
            var activeTrade = await Global.DataStore.GetActiveTradesAsync();
            var trade = activeTrade.FirstOrDefault(x => x.TradeId == tradeId);
            if (trade == null)
            {
                var closedTrades = await Global.DataStore.GetClosedTradesAsync();
                trade = closedTrades.FirstOrDefault(x => x.TradeId == tradeId);
            }

            return new JsonResult(trade);
        }

        [HttpGet]
        [Route("webSocketValues")]
        public IActionResult GetWebSocketValues()
        {
            return new JsonResult(Runtime.WebSocketTickers);

        }

        [HttpGet]
        [Route("sellNow/{tradeId}")]
        public async Task TradingSellNow(string tradeId)
        {
            var activeTrade = await Global.DataStore.GetActiveTradesAsync();
            var trade = activeTrade.FirstOrDefault(x => x.TradeId == tradeId);

            if (trade == null)
            {
                return;
            }

            var orderId = Global.Configuration.TradeOptions.PaperTrade ? Guid.NewGuid().ToString().Replace("-", "") : await Global.ExchangeApi.Sell(trade.Market, trade.Quantity, trade.TickerLast.Bid);
            trade.CloseRate = trade.TickerLast.Bid;
            trade.OpenOrderId = orderId;
            trade.SellOrderId = orderId;
            trade.SellType = SellType.Manually;
            trade.IsSelling = true;

            await Global.DataStore.SaveTradeAsync(trade);
            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", "Set " + tradeId + " to SellNow");
        }

        [HttpGet]
        [Route("cancelOrder/{tradeId}")]
        public async Task TradingCancelOrder(string tradeId)
        {
            var activeTrade = await Global.DataStore.GetActiveTradesAsync();
            var trade = activeTrade.FirstOrDefault(x => x.TradeId == tradeId);

            if (trade == null)
            {
                return;
            }

            if (trade.IsBuying)
            {
                await Global.ExchangeApi.CancelOrder(trade.BuyOrderId, trade.Market);
                trade.IsBuying = false;
                trade.OpenOrderId = null;
                trade.IsOpen = false;
                trade.SellType = SellType.Cancelled;
                trade.CloseDate = DateTime.UtcNow;
                await Global.DataStore.SaveTradeAsync(trade);
            }

            if (trade.IsSelling)
            {
                //Reenable in active trades
                await Global.ExchangeApi.CancelOrder(trade.SellOrderId, trade.Market);
                trade.IsSelling = false;
                trade.OpenOrderId = null;
                //trade.IsOpen = false;
                //trade.SellType = SellType.Cancelled;
                //trade.CloseDate = DateTime.UtcNow;
                await Global.DataStore.SaveTradeAsync(trade);
            }

            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", "Set " + tradeId + " to SellNow");
        }


        [HttpGet]
        [Route("hold/{tradeId}/{holdBoolean}")]
        public async Task TradingHold(string tradeId, bool holdBoolean)
        {
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            var tradeToUpdate = activeTrades.FirstOrDefault(x => x.TradeId == tradeId);
            if (tradeToUpdate != null)
            {
                tradeToUpdate.SellNow = false;
                tradeToUpdate.HoldPosition = holdBoolean;
                await Global.DataStore.SaveTradeAsync(tradeToUpdate);
            }

            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", "Set " + tradeId + " to Hold");
        }

        [HttpGet]
        [Route("sellOnProfit/{tradeId}/{profitPercentage}")]
        public async Task TradingSellOnProfit(string tradeId, decimal profitPercentage)
        {
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            var tradeToUpdate = activeTrades.FirstOrDefault(x => x.TradeId == tradeId);
            if (tradeToUpdate != null)
            {
                tradeToUpdate.SellNow = false;
                tradeToUpdate.HoldPosition = false;
                tradeToUpdate.SellOnPercentage = profitPercentage;

                await Global.DataStore.SaveTradeAsync(tradeToUpdate);
            }

            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", "Set " + tradeId + " to Hold");
        }

        [HttpGet]
        [Route("tradersTester")]
        public IActionResult MyntTradersTester()
        {
            JObject testJson = JObject.Parse(System.IO.File.ReadAllText("wwwroot/views/mynt_traders.json"));
            return new JsonResult(testJson);
        }

        [HttpGet]
        [Route("traders")]
        public async Task<IActionResult> Traders()
        {
            var traders = await Global.DataStore.GetTradersAsync();
            return new JsonResult(traders);
        }

        [HttpGet]
        [Route("activeTradesWithTrader")]
        public async Task<IActionResult> GetActiveTradesWithTrader()
        {
            // Get trades
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();

            JObject activeTradesJson = new JObject();

            // Get information for active trade
            foreach (var activeTrade in activeTrades)
            {
                activeTradesJson[activeTrade.TraderId] = JObject.FromObject(activeTrade);
            }

            return new JsonResult(activeTradesJson);
        }

        [HttpGet]
        [Route("activeTrades")]
        public async Task<IActionResult> GetActiveTrades()
        {
            // Get trades
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            return new JsonResult(activeTrades);
        }

        [HttpGet]
        [Route("openTrades")]
        public async Task<IActionResult> GetOpenTrades()
        {
            // Get trades
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            return new JsonResult(activeTrades.Where(x => x.IsSelling || x.IsBuying));
        }

        [HttpGet]
        [Route("closedTrades")]
        public async Task<IActionResult> GetClosedTrades()
        {
            // Get trades
            var closedTrades = await Global.DataStore.GetClosedTradesAsync();
            return new JsonResult(closedTrades);
        }
    }
}
