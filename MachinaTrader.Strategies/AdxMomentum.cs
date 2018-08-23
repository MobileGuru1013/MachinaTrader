using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class AdxMomentum : BaseStrategy
    {
        public override string Name => "ADX Momentum";
        public override int MinimumAmountOfCandles => 25;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var adx = candles.Adx(14);
            var diPlus = candles.PlusDi(25);
            var diMinus = candles.MinusDi(25);
            var sar = candles.Sar();
            var mom = candles.Mom(14);

            for (int i = 0; i < candles.Count; i++)
            {

                if (adx[i] > 25 && mom[i] < 0 && diMinus[i] > 25 && diPlus[i] < diMinus[i])
                    result.Add(TradeAdvice.Sell);
                else if (adx[i] > 25 && mom[i] > 0 && diPlus[i] > 25 && diPlus[i] > diMinus[i])
                    result.Add(TradeAdvice.Buy);
                else
                    result.Add(TradeAdvice.Hold);
            }

            return result;
        }

        public override TradeAdvice Forecast(List<Candle> candles)
        {
            return Prepare(candles).LastOrDefault();
        }

        public override Candle GetSignalCandle(List<Candle> candles)
        {
            return candles.Last();
        }
    }
}
