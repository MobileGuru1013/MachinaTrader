using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class FaMaMaMa : BaseStrategy
    {
        public override string Name => "FAMAMAMA";
        public override int MinimumAmountOfCandles => 20;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var mama = candles.Mama(0.5, 0.05);
            var fama = candles.Mama(0.25, 0.025);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Hold);
                
                else if (fama.Mama[i] > mama.Mama[i] && fama.Mama[i - 1] < mama.Mama[i])
                    result.Add(TradeAdvice.Buy);
                
                else if (fama.Mama[i] < mama.Mama[i] && fama.Mama[i - 1] > mama.Mama[i])
                    result.Add(TradeAdvice.Sell);
                
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
