using System;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Interfaces;

namespace MachinaTrader.Globals.Structure.Models
{
    public class TradeSignal
    {
        public Guid Id { get; set; }

        public Guid ParentId { get; set; }

        public string MarketName { get; set; }
        public string QuoteCurrency { get; set; }
        public string BaseCurrency { get; set; }
        public decimal Price { get; set; }
        public TradeAdvice TradeAdvice { get; set; }
        public Candle SignalCandle { get; set; }
		public ITradingStrategy Strategy { get; internal set; }

        public string StrategyName { get; set; }

        public decimal Profit { get; set; }
        public decimal PercentageProfit { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
