using System.Threading.Tasks;

namespace MachinaTrader.Globals.Data
{
    public interface IDatabaseInitializer
    {
        Task Initialize();
    }
}
