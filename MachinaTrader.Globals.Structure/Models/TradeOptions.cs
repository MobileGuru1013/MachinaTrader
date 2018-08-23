using System.Collections.Generic;
using MachinaTrader.Globals.Structure.Enums;

namespace MachinaTrader.Globals.Structure.Models
{
    public class Roi
    {
        public int Duration { get; set; }
        public decimal Profit { get; set; }
    }

    public class TradeOptions
    {
        decimal _stopLossPercentage = -0.07m;

        public bool BuyEnabled { get; set; } = true;
        public bool SellEnabled { get; set; } = true;

        public string BuyTimer { get; set; } = "0 0/5 * * * ?";
        public string SellTimer { get; set; } = "0 0/5 * * * ?";

        // Trading mode default is PaperTradeManager
        public bool PaperTrade { get; set; } = true;

        // Trader settings
        public int MaxNumberOfConcurrentTrades { get; set; } = 10;
        public decimal AmountToInvestPerTrader { get; set; } = 0.005m;
        public ProfitType ProfitStrategy { get; set; } = ProfitType.Reinvest;
        public decimal AmountToReinvestPercentage { get; set; } = 0.25m; //25% of wallet

        // If we go below this profit percentage, we sell immediately.
        public decimal StopLossPercentage
        {
            get { return _stopLossPercentage; }
            set
            {
                // Ensure it's a negative number.
                if (value > 0)
                    value = value * -1;

                _stopLossPercentage = value;
            }
        }

        // Use this to create a sell order as soon as the buy order is hit.
        // WARNING: This can't be used in combination with EnableTrailingStop.
        public bool ImmediatelyPlaceSellOrder { get; set; } = false;
        public decimal ImmediatelyPlaceSellOrderAtProfit { get; set; } = 0.02m;

        public bool OnlySellOnStrategySignals = false;

        // Use a trailing stop to lock in your profits.
        // WARNING: This can't be used in combination with ImmediatelyPlaceSellOrder.
        public bool EnableTrailingStop { get; set; } = true;
        public decimal TrailingStopStartingPercentage { get; set; } = 0.05m;
        public decimal TrailingStopPercentage { get; set; } = 0.05m;

        // If set to true, orders that have not been bought for an entire cycle of the BuyTimer
        // are cancelled. This frees up a trader to look for other opportunities.
        public bool CancelUnboughtOrdersEachCycle { get; set; } = true;

        // When enabled a first stop price is set to the current signal candle's low.
        public bool PlaceFirstStopAtSignalCandleLow { get; set; } = false;

        // Setting this to 0 means we will not look at volume and only look at our AlwaysTradeList. 
        // Setting this to any value higher than 0 means we will get a list of markets currently
        // trading a volume above this value and analyze those for buy signals.
        public int MinimumAmountOfVolume { get; set; } = 300;

        // Default strategy to use with trade managers.
        public string DefaultStrategy { get; set; } = "TheScalper";

        // Sets the bidding price. A value of 0.0 will use the ask price, 1.0 will use the last price and values between 
        // those interpolate between ask and last price. Using the ask price will guarantee quick success in bid, but
        // the bot will also end up paying more then would probably have been necessary.
        public BuyInPriceStrategy BuyInPriceStrategy { get; set; } = BuyInPriceStrategy.SignalCandleClose;
        public decimal AskLastBalance { get; set; } = 0.2m;
        public decimal BuyInPricePercentage { get; set; } = 0.005m;

        // A list of duration and profit pairs. The duration is a value in minutes and the profit is a 
        // decimal containing a percentage. This list is used to define constraints such as
        // "Sell when 5 minutes have passed and profit is at 3%".
        // WARNING: This can't be used in combination with ImmediatelyPlaceSellOrder.
        public List<Roi> ReturnOnInvestment { get; set; } = new List<Roi> { };

        // These are the markets we don't want to trade on
        public string QuoteCurrency { get; set; } = "BTC";

        // These are the markets we don't want to trade on
        public List<string> MarketBlackList { get; set; } = new List<string> { };

        // These are the markets we want to trade on regardless of volume
        public List<string> OnlyTradeList { get; set; } = new List<string> { };

        // These are the markets we want to trade on regardless of volume
        public List<string> AlwaysTradeList { get; set; } = new List<string> { };

    }

    public class DisplayOptions
    {
        // Display currency
        public string DisplayFiatCurrency { get; set; } = "USD";
    }
}
