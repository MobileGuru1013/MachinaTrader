using System;
using LiteDB;

namespace MachinaTrader.Data.LiteDB
{
    public class CandleAdapter
    {
        [BsonId]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
