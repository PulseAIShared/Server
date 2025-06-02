using Application.Abstractions.Messaging;
using Application.Notifications.Commands;
using Application.Notifications.Queries;
using DataTransferObjects.Common;
using DataTransferObjects.Notifications;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications
{
    internal sealed class MarkReadNotification : IEndpoint
    {

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
    
            app.MapPost("notifications/{notificationId:guid}/mark-read", async (
                Guid notificationId,
                ICommandHandler<MarkNotificationAsReadCommand, bool> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new MarkNotificationAsReadCommand(notificationId);
                Result<bool> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    success => Results.Ok(new { Message = "Notification marked as read" }),
                    CustomResults.Problem
                );
            })
            .RequireAuthorization()
            .WithTags("Notifications");
        }
    }
}
