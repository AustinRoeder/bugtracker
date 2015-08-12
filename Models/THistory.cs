using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace bug_tracker.Models
{
    public class THistory
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string EditId { get; set; }
        public string UserId { get; set; }
        public string Property { get; set; }
        public string PropertyDisplay { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string OldValueDisplay { get; set; }
        public string NewValueDisplay { get; set; }
        public DateTimeOffset Updated { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}