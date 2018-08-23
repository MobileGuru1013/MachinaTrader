using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class EmaAdxSmall : BaseStrategy
    {
        public override string Name => "EMA ADX Small";
        public override int MinimumAmountOfCandles => 15;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var closes = candles.Select(x => x.Close).ToList();
            var emaFast = candles.Ema(3);
            var emaSlow = candles.Ema(10);
            var minusDi = candles.MinusDi(14);
            var plusDi = candles.PlusDi(14);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Hold);
                
                else if (emaFast[i] > emaSlow[i] && (emaFast[i - 1] < emaSlow[i - 1] || plusDi[i - 1] < minusDi[i - 1]) && plusDi[i] > 20 && plusDi[i] > minusDi[i])
                    result.Add(TradeAdvice.Buy);
                
                else if (emaFast[i] < emaSlow[i] && emaFast[i - 1] > emaSlow[i - 1])
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
