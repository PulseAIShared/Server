using Application.Abstractions.Messaging;
using DataTransferObjects.Common;
using DataTransferObjects.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Queries
{
    public sealed record GetUserNotificationsQuery(
      int Page,
      int PageSize,
      bool UnreadOnly
  ) : IQuery<PagedResult<NotificationResponse>>;
}
