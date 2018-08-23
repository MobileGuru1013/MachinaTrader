using System.Collections.Generic;

namespace MachinaTrader.Globals.Structure.Models
{
    public class OrderBook
    {
        public List<OrderBookEntry> Asks { get; set; }
        public List<OrderBookEntry> Bids { get; set; }
    }
}
