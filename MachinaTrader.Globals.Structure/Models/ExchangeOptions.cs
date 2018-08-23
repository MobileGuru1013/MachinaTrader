using System;
using MachinaTrader.Globals.Structure.Enums;

namespace MachinaTrader.Globals.Structure.Models
{
    public class ExchangeOptions
    {
        public Exchange Exchange { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string PassPhrase { get; set; }

        public DateTime SimulationCurrentDate { get; set; }
        public bool IsSimulation { get; set; }
        public string SimulationCandleSize { get; set; } = "15";

        public decimal SimulationStartingWallet { get; set; } = 0.2m;
    }
}
