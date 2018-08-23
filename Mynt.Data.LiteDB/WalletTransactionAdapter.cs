using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using MachinaTrader.Globals.Structure.Enums;

namespace Mynt.Data.LiteDB
{
    public class WalletTransactionAdapter
    {
        [BsonId]
        public Guid Id { get; set; }

        public decimal Amount { get; set; }
       
        public DateTime Date { get; set; }
    }
}
