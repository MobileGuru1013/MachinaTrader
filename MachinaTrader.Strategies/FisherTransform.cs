using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class FisherTransform : BaseStrategy
    {
        public override string Name => "Fisher Transform";
        public override int MinimumAmountOfCandles => 40;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var fishers = candles.Fisher(10);
            var ao = candles.AwesomeOscillator();

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Hold);
                
                else if (fishers[i] < 0 && fishers[i - 1] > 0 && ao[i] < 0)
                    result.Add(TradeAdvice.Buy);
                
                else if (fishers[i] == 1)
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
