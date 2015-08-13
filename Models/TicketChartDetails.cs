using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace bug_tracker.Models
{
    public class TicketChartDetails
    {
        public string Title { get; set; }
        public int TicketCount { get; set; }
        public double PercentCompleted { get; set; }
        public IEnumerable<ApplicationUser> NotInRole { get; set; }
    }
}