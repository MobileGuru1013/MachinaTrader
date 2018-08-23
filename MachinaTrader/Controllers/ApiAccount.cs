using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MachinaTrader.Models;
using ExchangeSharp;
using MachinaTrader.Globals;
using Microsoft.AspNetCore.Authorization;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/account/")]
    public class ApiAccounts : Controller
    {
        /// <summary>
        /// Gets the balance for 1 Exchange
        /// Convert it so
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("balance")]
        public async Task<IActionResult> GetBalance()
        {
            //Account
            var account = new List<BalanceEntry>();

            // Some Options
            var displayOptions = Global.Configuration.DisplayOptions;

            // Get Exchange account
            var fullApi = Global.ExchangeApi.GetFullApi().Result;

            try
            {
                if (fullApi.PublicApiKey.Length > 0 && fullApi.PrivateApiKey.Length > 0)
                {
                    // Get Tickers & Balances
                    var tickers = await fullApi.GetTickersAsync();
                    var balances = await fullApi.GetAmountsAsync();
                    
                    if (tickers.Count() > 1)
                    {
                        var tickerUsd = tickers.Where(t => t.Key.ToUpper().Contains("USD") && t.Key.ToUpper().Contains("BTC")).ToList();
                        var tickerDisplayCurrency = tickers.Where(t => t.Key.ToUpper().Contains(displayOptions.DisplayFiatCurrency) && t.Key.ToUpper().Contains("BTC")).ToList();
                        var usdToBtcTicker = tickerUsd.First(t => t.Key.EndsWith("BTC"));
                        var btcToUsdTicker = tickerUsd.First(t => t.Key.StartsWith("BTC"));

                        var dcTicker = new KeyValuePair<string,ExchangeTicker>();
                        if (tickerDisplayCurrency.Count == 0)
                            Global.Logger.Information("Account: Display currency at this exchange not available!");
                        else
                            dcTicker = tickerDisplayCurrency.First(t => t.Key.StartsWith("BTC"));

                        // Calculate stuff
                        foreach (var balance in balances)
                        {
                            // Get selected tickers for Balances
                            var ticker = new List<KeyValuePair<string, ExchangeTicker>>();

                            // Create balanceEntry
                            var balanceEntry = new BalanceEntry()
                            {
                                DisplayCurrency = displayOptions.DisplayFiatCurrency,
                                Market = balance.Key,
                                TotalCoins = balance.Value,
                                BalanceInUsd = 0,
                                BalanceInBtc = 0,
                                BalanceInDisplayCurrency = 0
                            };

                            // Calculate to BTC or USD and check if crypto or not
                            if (!balance.Key.ToUpper().Contains("USD") && !balance.Key.ToUpper().Contains("EUR"))
                            {
                                // Calculate to btc
                                if (!balance.Key.ToUpper().Contains("BTC"))
                                    ticker = tickers.Where(t =>
                                            t.Key.ToUpper().StartsWith(balance.Key) &&
                                            t.Key.ToUpper().Contains("BTC"))
                                        .ToList();
                            }
                            // Calculate to btc
                            else
                                ticker = tickers.Where(t =>
                                        t.Key.ToUpper().StartsWith(balance.Key) && t.Key.ToUpper().Contains("BTC"))
                                    .ToList();

                            // Calculate special market USD, EUR, BTC
                            if (balanceEntry.Market.ToUpper().Contains("USD") ||
                                balanceEntry.Market.ToUpper().Contains("EUR") ||
                                balanceEntry.Market.ToUpper().Contains("BTC"))
                            {
                                if (balanceEntry.Market.ToUpper().Contains("USD"))
                                {
                                    balanceEntry.BalanceInUsd = balance.Value;
                                    balanceEntry.BalanceInBtc = balanceEntry.BalanceInUsd * usdToBtcTicker.Value.Last;
                                }

                                if (balanceEntry.Market.ToUpper().Contains("EUR"))
                                {
                                    var t = "";
                                }

                                if (balanceEntry.Market.ToUpper().Contains("BTC"))
                                {
                                    balanceEntry.BalanceInBtc = balance.Value;
                                    balanceEntry.BalanceInUsd = balanceEntry.BalanceInBtc * btcToUsdTicker.Value.Last;
                                }

                                if (tickerDisplayCurrency.Count >= 1)
                                    balanceEntry.BalanceInDisplayCurrency =
                                        balanceEntry.BalanceInBtc * dcTicker.Value.Last;
                            }
                            // Calculate cryptos without btc
                            else
                            {
                                balanceEntry.BalanceInBtc = (balance.Value * ticker[0].Value.Last);
                                balanceEntry.BalanceInUsd = balanceEntry.BalanceInBtc * btcToUsdTicker.Value.Last;
                                if (tickerDisplayCurrency.Count >= 1)
                                    balanceEntry.BalanceInDisplayCurrency =
                                        balanceEntry.BalanceInBtc * dcTicker.Value.Last;
                            }

                            // Add to list
                            account.Add(balanceEntry);
                        }
                    }
                    else
                        Global.Logger.Information("Possible problem with API, cuz we got no tickers!");
                }
                else
                    Global.Logger.Information("No api under configuration");
            }
            catch (Exception e)
            {
                Global.Logger.Error(e.InnerException.Message);
            }
            
            return new JsonResult(account);
        }
    }
}
