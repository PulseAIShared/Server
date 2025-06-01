using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Enums
{
    public enum CampaignTrigger
    {
        Manual,
        PaymentFailed,
        HighChurnRisk,
        InactiveUser,
        TrialEnding,
        FeatureLimitReached
    }
}
