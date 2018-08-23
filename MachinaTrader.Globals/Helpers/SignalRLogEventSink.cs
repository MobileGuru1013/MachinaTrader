using System;
using MachinaTrader.Globals.Hubs;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection;

namespace MachinaTrader.Globals.Helpers
{
    public class SignalRLogEventSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public SignalRLogEventSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < LogEventLevel.Information)
                return;

            if (logEvent.Properties.TryGetValue("RequestPath", out var dd))
            {
                if (dd.ToString().StartsWith("\"/signalr/"))
                    return;

                if (dd.ToString().StartsWith("\"/api/logging/logs"))
                    return;
            }

            var message = logEvent.RenderMessage(_formatProvider);
            //Console.WriteLine("FROM MySink " + DateTimeOffset.Now.ToString() + " " + message);

            if (Global.ServiceScope != null)
            {
                if (Global.GlobalHubLogs == null)
                {
                    Global.GlobalHubLogs = Global.ServiceScope.ServiceProvider.GetService<IHubContext<HubLogs>>();
                }

                Global.GlobalHubLogs.Clients.All.SendAsync("Send", message);
            }
        }
    }

    public static class MySinkExtensions
    {
        public static LoggerConfiguration SignalRLogEventSink(this LoggerSinkConfiguration loggerConfiguration, IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new SignalRLogEventSink(formatProvider));
        }
    }
}
