using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace bug_tracker.Models
{
    public class TComment
    {
        public int ID { get; set; }
        public int TicketID { get; set; }
        public string Body { get; set; }
        public string OwnerID { get; set; }
        public DateTimeOffset Created { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser Owner { get; set; }
    }
}