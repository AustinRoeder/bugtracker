using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace bug_tracker.Models
{
    public class TComment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Body { get; set; }
        public string OwnerId { get; set; }
        public DateTimeOffset Created { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser Owner { get; set; }
    }
}