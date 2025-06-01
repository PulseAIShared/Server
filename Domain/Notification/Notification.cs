using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Notification
{
    public class Notification : Entity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationCategory Category { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        public Users.User User { get; set; } = null!;

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }
}
