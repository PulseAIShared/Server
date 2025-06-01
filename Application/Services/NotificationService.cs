using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class NotificationService(
      IHubContext<NotificationHub> hubContext,
      ILogger<NotificationService> logger) : INotificationService
    {
        public async Task SendNotificationToUserAsync(Guid userId, string eventType, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                await hubContext.Clients.User(userId.ToString()).SendAsync(eventType, data, cancellationToken);
                logger.LogInformation("Sent real-time notification to user {UserId} for event {EventType}", userId, eventType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send real-time notification to user {UserId}", userId);
            }
        }

        public async Task SendNotificationToUsersAsync(IEnumerable<Guid> userIds, string eventType, object data, CancellationToken cancellationToken = default)
        {
            var userIdStrings = userIds.Select(id => id.ToString()).ToList();

            try
            {
                await hubContext.Clients.Users(userIdStrings).SendAsync(eventType, data, cancellationToken);
                logger.LogInformation("Sent real-time notification to {UserCount} users for event {EventType}", userIdStrings.Count, eventType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send real-time notification to multiple users");
            }
        }
    }

}
