using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace bug_tracker.Models
{
    public class TAttachment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string FileUrl { get; set; }
        public string FileExt { get; set; }
        public string UserId { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTimeOffset Created { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}