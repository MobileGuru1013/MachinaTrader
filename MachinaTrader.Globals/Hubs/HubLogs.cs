using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MachinaTrader.Globals.Hubs
{
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
    public class HubLogs : Hub
    {
    }
}
