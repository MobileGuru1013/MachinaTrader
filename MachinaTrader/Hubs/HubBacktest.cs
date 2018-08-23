using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MachinaTrader.Hubs
{
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
    public class HubBacktest : Hub
    {
    }
}
