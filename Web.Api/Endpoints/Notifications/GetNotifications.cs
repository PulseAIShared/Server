
using Application.Abstractions.Messaging;
using Application.Notifications.Commands;
using Application.Notifications.Queries;
using DataTransferObjects.Common;
using DataTransferObjects.Notifications;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications
{
    internal sealed class GetNotifications : IEndpoint
    {

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("notifications", async (
                IQueryHandler<GetUserNotificationsQuery, PagedResult<NotificationResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                [FromQuery] bool unreadOnly = false) =>
            {
                var query = new GetUserNotificationsQuery(page, pageSize, unreadOnly);
                Result<PagedResult<NotificationResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(
                    pagedResult => Results.Ok(new
                    {
                        Notifications = pagedResult.Items.Select(n => new NotificationResponse(
                            n.Id,
                            n.Title,
                            n.Message,
                            n.Type,
                            n.Category,
                            n.ActionUrl,
                            n.ActionText,
                            n.IsRead,
                            n.CreatedAt,
                            n.Metadata
                        )),
                        TotalCount = pagedResult.TotalCount,
                        Page = pagedResult.Page,
                        PageSize = pagedResult.PageSize,
                        TotalPages = pagedResult.TotalPages
                    }),
                    CustomResults.Problem
                );
            })
            .RequireAuthorization()
            .WithTags("Notifications");

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
