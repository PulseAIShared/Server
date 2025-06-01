using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Models
{
    public class MailchimpMember
    {
        public string Id { get; set; } = string.Empty;
        public string Email_address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public MailchimpMemberStats Stats { get; set; } = new();
        public DateTime Timestamp_signup { get; set; }
        public DateTime Timestamp_opt { get; set; }
        public DateTime Last_changed { get; set; }
        public Dictionary<string, object> Merge_fields { get; set; } = new();
    }

    public class MailchimpMemberStats
    {
        public double Avg_open_rate { get; set; }
        public double Avg_click_rate { get; set; }
        public int Campaign_count { get; set; }
    }

    public class MailchimpCampaign
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Create_time { get; set; }
        public DateTime? Send_time { get; set; }
        public MailchimpCampaignSettings Settings { get; set; } = new();
        public MailchimpReportSummary Report_summary { get; set; } = new();
    }

    public class MailchimpCampaignSettings
    {
        public string Subject_line { get; set; } = string.Empty;
        public string From_name { get; set; } = string.Empty;
        public string Reply_to { get; set; } = string.Empty;
    }

    public class MailchimpReportSummary
    {
        public int Opens { get; set; }
        public int Unique_opens { get; set; }
        public double Open_rate { get; set; }
        public int Clicks { get; set; }
        public int Subscriber_clicks { get; set; }
        public double Click_rate { get; set; }
    }
}

