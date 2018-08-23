using System;
using MachinaTrader.Globals.Structure.Enums;

namespace MachinaTrader.Globals.Structure.Models
{
    public class Trade
    {
        // Used as primary key for the different data storage mechanisms.
        public int Id { get; set; }

        public string TradeId { get; set; }
        public string TraderId { get; set; }
        public string Market { get; set; }

        public decimal OpenRate { get; set; }
        public decimal? CloseRate { get; set; }
        public decimal? CloseProfit { get; set; }
        public decimal? CloseProfitPercentage { get; set; }

        public decimal StakeAmount { get; set; }
        public decimal Quantity { get; set; }

        public bool IsOpen { get; set; }
        public bool IsBuying { get; set; }
        public bool IsSelling { get; set; }

        public string OpenOrderId { get; set; }
        public string BuyOrderId { get; set; }
        public string SellOrderId { get; set; }

        public DateTime OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }

        public string StrategyUsed { get; set; }
        public decimal? StopLossRate { get; set; }
        public SellType SellType { get; set; }

        public bool PaperTrading { get; set; }

        public Trade()
        {
            TradeId = Guid.NewGuid().ToString().Replace("-", string.Empty);
            IsOpen = true;
            OpenDate = DateTime.UtcNow;
        }

        // Used for MyntUI output
        public Ticker TickerLast { get; set; }

        //Add Options for this trade
        public decimal SellOnPercentage { get; set; } = (decimal)0.0;
        public bool HoldPosition { get; set; } = false;
        public bool SellNow { get; set; } = false;
        public string GlobalSymbol { get; set; }
        public string Exchange { get; set; }
        public bool PaperTrade { get; set; } = true;
    }
}
