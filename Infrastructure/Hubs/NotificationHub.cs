using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;


namespace Infrastructure.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public async Task JoinUserGroup()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
        }

        public async Task LeaveUserGroup()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
        }

        public override async Task OnConnectedAsync()
        {
            await JoinUserGroup();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await LeaveUserGroup();
            await base.OnDisconnectedAsync(exception);
        }
    }
}
