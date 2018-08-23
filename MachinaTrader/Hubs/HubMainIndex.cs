using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MachinaTrader.Hubs
{
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
    public class HubMainIndex : Hub
    {
        /*
        public override Task OnConnectedAsync()
        {
            // Context.User.Identity.Name always null but this work in asp.net 4.6
            var id = Context.ConnectionId;
            var name = Context.User.Identity.Name;
            Console.WriteLine(id);
            Console.WriteLine(name);
            Console.WriteLine("OnConnected " + Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var id = Context.ConnectionId;
            var name = Context.User.Identity.Name;
            Console.WriteLine(id);
            Console.WriteLine(name);
            Console.WriteLine("OnDisconnected " + Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        /*public override async Task OnConnectedAsync()
        {
            Console.WriteLine(Global.RuntimeSettings["signalrClients"]);
            string name = Context.User.Identity.Name;
            Console.WriteLine(name);
            Console.WriteLine("OnConnected " + Context.ConnectionId);
        }
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            Console.WriteLine("OnDisconnected " + Context.ConnectionId);
        }*/



    }
}
