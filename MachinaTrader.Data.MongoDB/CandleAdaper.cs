using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MachinaTrader.Data.MongoDB
{
    [BsonIgnoreExtraElements]
    public class CandleAdapter
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
