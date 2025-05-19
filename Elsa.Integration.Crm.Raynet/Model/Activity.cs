using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
namespace Elsa.Integration.Crm.Raynet.Model
{

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Activity
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public IdContainer Company { get; set; }
        public DateTime? ScheduledFrom { get; set; } // yyyy-MM-dd HH:mm
        public DateTime? ScheduledTill { get; set; } // yyyy-MM-dd HH:mm
        public DateTime? Completed { get; set; } // yyyy-MM-dd
        public string Description { get; set; }
        public List<ActivityParticipant> Participants { get; set; }
        public ActivityCategory Category { get; set; }       
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ActivityParticipant
    {
        public string Name { get; set; }
        public long Person { get; set; }
        public string Role { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ActivityCategory
    {
        public long Id { get; set; }
        public string Value { get; set; }
    }
}
