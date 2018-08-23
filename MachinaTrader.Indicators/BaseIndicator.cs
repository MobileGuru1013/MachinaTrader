using System.Collections.Generic;
using System.Linq;

namespace MachinaTrader.Indicators
{
    public static partial class Extensions
    {
        private static List<decimal?> FixIndicatorOrdering(List<double> items, int outBegIdx, int outNbElement)
        {
            var outValues = new List<decimal?>();
            var validItems = items.Take(outNbElement);

            for (int i = 0; i < outBegIdx; i++)
                outValues.Add(null);

            foreach (var value in validItems)
                outValues.Add((decimal?)value);

            return outValues;
        }
    }
}
