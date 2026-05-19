using Microsoft.AspNetCore.SignalR;

namespace SinfraRMM.API.Hubs
{
    public class MonitorHub : Hub
    {
        // El MVC se conecta y se une al grupo de su servidor
        // Así solo recibe notificaciones del servidor que le interesa
        public async Task JoinServerGroup(string serverId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"server-{serverId}");
        }

        public async Task LeaveServerGroup(string serverId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"server-{serverId}");
        }
    }
}