using System;

namespace MachinaTrader.Globals.Structure.Models
{
    public class WalletTransaction
    {
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }          
    }
}
