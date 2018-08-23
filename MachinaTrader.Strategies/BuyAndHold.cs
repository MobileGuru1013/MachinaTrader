using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Strategies
{
    public class BuyAndHold : BaseStrategy
    {
        public override string Name => "Buy and Hold";
        public override int MinimumAmountOfCandles => 20;
        public override Period IdealPeriod => Period.QuarterOfAnHour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice> { TradeAdvice.Buy };
            var holdAdvices = new TradeAdvice[candles.Count - 2];

            result.AddRange(holdAdvices);
            result.Add(TradeAdvice.Sell);

            return result;
        }

        public override Candle GetSignalCandle(List<Candle> candles)
        {
            return candles.Last();
        }

        public override TradeAdvice Forecast(List<Candle> candles)
        {
            return Prepare(candles).LastOrDefault();
        }
    }
}
