using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MachinaTrader.Globals.Helpers
{
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
}
