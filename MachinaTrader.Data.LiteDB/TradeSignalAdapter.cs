using System;
using LiteDB;
using MachinaTrader.Globals.Structure.Enums;

namespace MachinaTrader.Data.LiteDB
{
    public class TradeSignalAdapter
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid ParentId { get; set; }

        public string MarketName { get; set; }
        public string QuoteCurrency { get; set; }
        public string BaseCurrency { get; set; }
        public decimal Price { get; set; }
        public TradeAdvice TradeAdvice { get; set; }
        public CandleAdapter SignalCandle { get; set; }

        public string StrategyName { get; internal set; }
        public decimal Profit { get; set; }
        public decimal PercentageProfit { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
