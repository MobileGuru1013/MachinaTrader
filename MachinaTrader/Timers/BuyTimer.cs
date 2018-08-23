using System.Threading.Tasks;
using MachinaTrader.TradeManagers;
using Quartz;
using MachinaTrader.Globals;

namespace MachinaTrader.Timers
{
    [DisallowConcurrentExecution]
    public class BuyTimer : IJob
    {
        /// <summary>
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            var tradeManager = new TradeManager();

            Global.Logger.Information("Mynt service is looking for new trades.");
            await tradeManager.LookForNewTrades();
            await Task.FromResult(true);
        }
    }
}
