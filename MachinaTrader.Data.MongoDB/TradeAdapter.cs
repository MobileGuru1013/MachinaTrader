using System;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MachinaTrader.Data.MongoDB
{
    public class TradeAdapter
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }
        public string TradeId { get; set; }
        public string TraderId { get; set; }
        public string Market { get; set; }

        public double OpenRate { get; set; }
        public double? CloseRate { get; set; }
        public double? CloseProfit { get; set; }
        public double? CloseProfitPercentage { get; set; }

        public double StakeAmount { get; set; }
        public double Quantity { get; set; }

        public bool IsOpen { get; set; }
        public bool IsBuying { get; set; }
        public bool IsSelling { get; set; }

        public string OpenOrderId { get; set; }
        public string BuyOrderId { get; set; }
        public string SellOrderId { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime OpenDate { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CloseDate { get; set; }

        public string StrategyUsed { get; set; }
        public double? StopLossRate { get; set; }
        public SellType SellType { get; set; }

        // Used for MyntUI output
        public Ticker TickerLast { get; set; }

        //Add Options for this trade
        public decimal SellOnPercentage { get; set; }
        public bool HoldPosition { get; set; }
        public bool SellNow { get; set; }
        public string GlobalSymbol { get; set; }
        public string Exchange { get; set; }
        public string PaperTrade { get; set; }
    }
}
