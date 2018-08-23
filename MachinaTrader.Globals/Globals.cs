using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using LazyCache;
using MachinaTrader.Globals.Hubs;
using MachinaTrader.Globals.Models;
using MachinaTrader.Globals.Structure.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using LogLevel = Quartz.Logging.LogLevel;

namespace MachinaTrader.Globals
{
    //Define Global Variables 
    public static class Global
    {
        public static void InitGlobals()
        {
            //Read Default MainSettings
            var settings = new GlobalSettings();
            settings.Folders();
            settings.LogConfiguration();
            settings.DefaultCoreSettings();
            settings.DefaultCoreRuntimeSettings();
            settings.CommonFiles();
        }

        public static string GetOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "OSX";
            }

            return "Unknown";
        }

        public sealed class QuartzScheduler
        {
            private static QuartzScheduler _instance;

            private static readonly object Padlock = new object();

            private readonly ISchedulerFactory _schedulerFactory;
            private readonly IScheduler _scheduler;

            QuartzScheduler()
            {
                _schedulerFactory = new StdSchedulerFactory();
                _scheduler = _schedulerFactory.GetScheduler().Result;
            }

            public static IScheduler Instance
            {
                get
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new QuartzScheduler();
                        }
                        return _instance._scheduler;
                    }
                }
            }
        }

        //Setup Quartz Logger to console -> Without this LibLog  will trigger FileNotFound Exceptions on each run
        public class QuartzConsoleLogProvider : ILogProvider
        {
            public Logger GetLogger(string name)
            {
                return (level, func, exception, parameters) =>
                {
                    if (level >= LogLevel.Info && func != null)
                    {
                        Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);
                    }
                    return true;
                };
            }

            public IDisposable OpenNestedContext(string message)
            {
                throw new NotImplementedException();
            }

            public IDisposable OpenMappedContext(string key, string value)
            {
                throw new NotImplementedException();
            }
        }

        public static string AppPath = "";
        public static string AppParentPath = "";
        public static string DataPath = "";
        public static bool WebServerReady = false;

        public static JObject CoreConfig = new JObject();
        public static JObject CoreRuntime = new JObject();

        public static IDataStore DataStore { get; set; }
        public static IDataStoreBacktest DataStoreBacktest { get; set; }
        public static IExchangeApi ExchangeApi { get; set; }
        public static MainConfig Configuration { get; set; }
        public static IScheduler QuartzTimer = new StdSchedulerFactory().GetScheduler().Result;
        public static string DatabaseConnectionString { get; set; }

        public static RuntimeConfig RuntimeSettings = new RuntimeConfig();
        public static IApplicationBuilder ApplicationBuilder { get; set; }
        public static IServiceScope ServiceScope { get; set; }
        public static Serilog.ILogger Logger { get; set; }
        public static IAppCache AppCache { get; set; }
        public static IHubContext<HubLogs> GlobalHubLogs;
    }
}
