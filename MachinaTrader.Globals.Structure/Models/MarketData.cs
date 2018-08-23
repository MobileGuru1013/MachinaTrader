using System.Collections.Generic;

namespace MachinaTrader.Globals.Structure.Models
{
    public class MarketData
    {
        public string Name { get; set; }
        public List<Candle> Candles { get; set; }
        public List<int> Trend { get; set; }
    }
}
