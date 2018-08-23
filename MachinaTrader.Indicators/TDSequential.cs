using System.Collections.Generic;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Indicators
{

    public static partial class Extensions
	{
		public static List<TdSeq> TdSequential(this List<Candle> source)
        {
			var result = new List<TdSeq>();
			var close = source.Close();
			var td = new List<int>();
			var ts = new List<int>();
			var tdUps = new List<int?>();
			var tdDowns = new List<int?>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i < 4)
				{
					td.Add(0);
					ts.Add(0);
					result.Add(null);
					continue;
				}

				var currentTd = close[i] > close[i - 4] ? td[i - 1] + 1 : 0;
				var currentTs = close[i] < close[i - 4] ? ts[i - 1] + 1 : 0;
                td.Add(currentTd);
                ts.Add(currentTs);

				var tdUp = currentTd - ValueWhenPreviousLower(td);
				var tdDown = currentTs - ValueWhenPreviousLower(ts);

				tdUps.Add(tdUp);
				tdDowns.Add(tdDown);

				if (tdUp > 0 && tdUp <= 9)
					result.Add(new TdSeq{ Value = tdUp , IsGreen = true});
				else if (tdDown > 0 && tdDown <= 9)
					result.Add(new TdSeq { Value = tdDown, IsGreen = false });
				else
					result.Add(new TdSeq { Value = null, IsGreen = true });
			}

			return result;
		}

		private static int? ValueWhenPreviousLower(List<int> values)
		{
			var counter = 0;

			for (int i = 0; i < values.Count; i++)
			{
				if (i == 0)
					continue;

				if (values[i] < values[i - 1] && counter == 1)
					return values[i];
				else if (values[i] < values[i - 1])
					counter += 1;
			}

			return null;
		}
	}

    public class TdSeq
    {
        public int? Value { get; set; }
        public bool IsGreen { get; set; }

		public override string ToString()
		{
			return $"{Value}";
		}
	}
}
