using System;

namespace MachinaTrader.Models
{
    public class LogEntry
    {
        // Profit - loss
        public DateTime Date { get; set; }
        public string LogState { get; set; }
        public string Msg { get; set; }
    }
}
