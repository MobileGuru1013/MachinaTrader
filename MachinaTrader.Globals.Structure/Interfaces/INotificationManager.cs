using System.Threading.Tasks;

namespace MachinaTrader.Globals.Structure.Interfaces
{
    public interface INotificationManager
    {
        Task<bool> SendNotification(string message);
        Task<bool> SendTemplatedNotification(string template, params object[] parameters);
    }
}
