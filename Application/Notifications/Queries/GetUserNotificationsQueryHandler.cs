using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Common;
using DataTransferObjects.Notifications;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Queries
{
    internal sealed class GetUserNotificationsQueryHandler(
      IApplicationDbContext context,
      IUserContext userContext)
      : IQueryHandler<GetUserNotificationsQuery, PagedResult<NotificationResponse>>
    {
        public async Task<Result<PagedResult<NotificationResponse>>> Handle(GetUserNotificationsQuery query, CancellationToken cancellationToken)
        {
            var notificationsQuery = context.Notifications
                .Where(n => n.UserId == userContext.UserId);

            if (query.UnreadOnly)
            {
                notificationsQuery = notificationsQuery.Where(n => !n.IsRead);
            }

            var totalCount = await notificationsQuery.CountAsync(cancellationToken);

            var notifications = await notificationsQuery
                .OrderByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    Category = n.Category.ToString(),
                    ActionUrl = n.ActionUrl,
                    ActionText = n.ActionText,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Metadata = n.Metadata
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResult<NotificationResponse>
            {
                Items = notifications,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };

            return result;
        }
    }
}
