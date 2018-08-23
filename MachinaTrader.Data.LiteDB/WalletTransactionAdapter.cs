using System;
using LiteDB;

namespace MachinaTrader.Data.LiteDB
{
    public class WalletTransactionAdapter
    {
        [BsonId]
        public Guid Id { get; set; }

        public decimal Amount { get; set; }
       
        public DateTime Date { get; set; }
    }
}
