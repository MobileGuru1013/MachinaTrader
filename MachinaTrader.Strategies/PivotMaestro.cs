using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class PivotMaestro : BaseStrategy
    {
        public override string Name => "PivotMaestro";
        public override int MinimumAmountOfCandles => 10;
        public override Period IdealPeriod => Period.QuarterOfAnHour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var high = candles.PivotHigh(4, 2, false);
            var low = candles.PivotLow(4, 2, false);
            var lows = candles.Low();

            for (int i = 0; i < candles.Count; i++)
            {
                // Buy when a lower pivot was found.
                if (low[i].HasValue)
                    result.Add(TradeAdvice.Buy);

                // Either a upper pivot or a new potential low pivot should make us sell.
                else if (high[i].HasValue || (i > 3 && (lows[i] <= lows[i - 1] && lows[i] <= lows[i - 2] && lows[i] <= lows[i - 3] && lows[i] <= lows[i - 4])))
                    result.Add(TradeAdvice.Sell);

                // Hold otherwise
                else
                    result.Add(TradeAdvice.Hold);
            }

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
