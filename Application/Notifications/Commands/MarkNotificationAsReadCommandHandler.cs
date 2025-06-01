using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Commands
{
    internal sealed class MarkNotificationAsReadCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : ICommandHandler<MarkNotificationAsReadCommand, bool>
    {
        public async Task<Result<bool>> Handle(MarkNotificationAsReadCommand command, CancellationToken cancellationToken)
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Id == command.NotificationId, cancellationToken);

            if (notification == null)
            {
                return Result.Failure<bool>(Error.NotFound(
                    "Notification.NotFound",
                    $"Notification with ID {command.NotificationId} was not found"));
            }

            if (notification.UserId != userContext.UserId)
            {
                return Result.Failure<bool>(Error.Failure(
                    "Notification.Unauthorized",
                    "You are not authorized to access this notification"));
            }

            if (!notification.IsRead)
            {
                notification.MarkAsRead();
                await context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }

}
