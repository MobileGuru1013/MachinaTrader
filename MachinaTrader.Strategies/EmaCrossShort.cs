using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class EmaCrossShort : BaseStrategy
    {
        public override string Name => "EMA Cross Short";
        public override int MinimumAmountOfCandles => 36;
        public override Period IdealPeriod => Period.FiveMinutes;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            try
            {
                var shorts = candles.Ema(6);
                var longs = candles.Ema(3);

                for (int i = 0; i < candles.Count; i++)
                {
                    var diff = 100 * (shorts[i] - longs[i]) / ((shorts[i] + longs[i]) / 2);

                    if (diff > 1.5m)
                    {
                        result.Add(TradeAdvice.Buy);
                    }
                    else if (diff <= -0.1m)
                    {
                        result.Add(TradeAdvice.Sell);
                    }
                    else
                    {
                        result.Add(TradeAdvice.Hold);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Global.Logger.Error(ex.ToString());
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
