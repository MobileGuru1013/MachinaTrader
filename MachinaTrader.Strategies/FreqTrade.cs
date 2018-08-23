using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class FreqTrade : BaseStrategy
    {
        public override string Name => "FreqTrade";
        public override int MinimumAmountOfCandles => 40;
        public override Period IdealPeriod => Period.QuarterOfAnHour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            //Global.Logger.Information($"Starting FreqTrade");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            var result = new List<TradeAdvice>();

            var rsi = candles.Rsi(14);
            var adx = candles.Adx(14);
            var plusDi = candles.PlusDi(14);
            var minusDi = candles.MinusDi(14);
            var fast = candles.StochFast();

            for (int i = 0; i < candles.Count; i++)
            {
                if (
                    rsi[i] < 25
                    && fast.D[i] < 30
                    && adx[i] > 30
                    && plusDi[i] > 5
                    )
                    result.Add(TradeAdvice.Buy);

                else if (
                    adx[i] > 0
                    && minusDi[i] > 0
                    && fast.D[i] > 65
                    )
                    result.Add(TradeAdvice.Sell);

                else
                    result.Add(TradeAdvice.Hold);
            }

            //if (_logger != null)
            //{
            //    try
            //    {
            //        _logger.Information("{Name} " +
            //                               "rsi:{rsi} ," +
            //                               "fast.D:{f} ," +
            //                               "adx:{a} ," +
            //                               "plusDi:{p} ",
            //                               Name,
            //                               rsi[candles.Count - 1],
            //                               fast.D[candles.Count - 1],
            //                               adx[candles.Count - 1],
            //                               plusDi[candles.Count - 1]);

            //    }
            //    catch (Exception ex)
            //    {
            //        Global.Logger.Error(ex.ToString());
            //    }
            //}

            //watch1.Stop();
            //Global.Logger.Warning($"Ended FreqTrade in #{watch1.Elapsed.TotalSeconds} seconds");

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
