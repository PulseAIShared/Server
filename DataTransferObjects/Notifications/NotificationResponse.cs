using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Notifications
{
    public sealed class NotificationResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public NotificationResponse() { }

        public NotificationResponse(
            Guid id,
            string title,
            string message,
            string type,
            string category,
            string? actionUrl,
            string? actionText,
            bool isRead,
            DateTime createdAt,
            Dictionary<string, object>? metadata)
        {
            Id = id;
            Title = title;
            Message = message;
            Type = type;
            Category = category;
            ActionUrl = actionUrl;
            ActionText = actionText;
            IsRead = isRead;
            CreatedAt = createdAt;
            Metadata = metadata;
        }
    }
}
