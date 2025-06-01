using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface INotificationService
    {
        Task SendNotificationToUserAsync(Guid userId, string eventType, object data, CancellationToken cancellationToken = default);
        Task SendNotificationToUsersAsync(IEnumerable<Guid> userIds, string eventType, object data, CancellationToken cancellationToken = default);
    }
}
