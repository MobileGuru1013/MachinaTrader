using System;
using System.Collections.Generic;
using System.Linq;

namespace MachinaTrader.Models
{
    public class Statistics
    {
        public decimal? ProfitLoss { get; set; } = 0;
        public decimal? ProfitLossPercentage { get; set; } = 0;

        public List<CoinPerformance> CoinPerformances { get; set; }
    }

    public class CoinPerformance
    {
        public string Coin { get; set; }
        public decimal? Performance { get; set; } = 0;
        public decimal? PerformancePercentage { get; set; } = 0;
        public int? PositiveTrades { get; set; }
        public int? NegativeTrade { get; set; }
    }

    public class WalletStatistic
    {
        public List<DateTime> Dates { get; set; }

        public List<decimal> Amounts { get; set; }

        public List<decimal> Balances { get; set; }
    }
}
