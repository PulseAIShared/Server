using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Enums
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public enum NotificationCategory
    {
        System,
        Import,
        Export,
        Campaign,
        Churn,
        Payment,
        Integration
    }
}
