using System;
using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;
using Serilog;

namespace MachinaTrader.Strategies
{
    public class FreqTradeEvo : BaseStrategy
    {
        private ILogger _logger;

        public override string Name => "FreqTrade Evo";
        public override int MinimumAmountOfCandles => 40;
        public override Period IdealPeriod => Period.QuarterOfAnHour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var rsi = candles.Rsi(5);
            var fast = candles.StochFast();
            var bb = candles.Bbands(20);

            var adx = candles.Adx(14);
            var plusDi = candles.PlusDi(14);
            var minusDi = candles.MinusDi(14);

            for (int i = 0; i < candles.Count; i++)
            {
                if (
                    rsi[i] < 22
                    && fast.K[i] < 25
                    // && bb.LowerBand[i] > candles[i].Close
                    && (fast.D[i - 1] > fast.K[i - 1])
                    && ((fast.D[i] - fast.K[i]) < 0.3m)
                    )
                    result.Add(TradeAdvice.Buy);

                else if (
                     rsi[i] > 70
                     && fast.K[i] > 50
                    )
                    result.Add(TradeAdvice.Sell);

                else
                    result.Add(TradeAdvice.Hold);
            }

            if (_logger != null)
            {
                try
                {
                    _logger.Information("{Name} " +
                                           "rsi:{rsi} ," +
                                           "fast.D:{f} ," +
                                           "adx:{a} ," +
                                           "plusDi:{p} ",
                                           Name,
                                           rsi[candles.Count - 1],
                                           fast.D[candles.Count - 1],
                                           adx[candles.Count - 1],
                                           plusDi[candles.Count - 1]);

                }
                catch (Exception ex)
                {
                    Global.Logger.Error(ex.ToString());
                }
            }

            return result;
        }

        public override Candle GetSignalCandle(List<Candle> candles)
        {
            return candles.Last();
        }

        public override TradeAdvice Forecast(List<Candle> candles, ILogger logger)
        {
            _logger = logger;
            return Prepare(candles).LastOrDefault();
        }
    }
}
