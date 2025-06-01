using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Enums
{
    public enum ActivityType
    {
        Login,
        FeatureUsage,
        PaymentSuccess,
        PaymentFailure,
        SupportTicket,
        EmailOpened,
        EmailClicked,
        CampaignInteraction,
        SubscriptionChange,
        AccountUpdated
    }
}
