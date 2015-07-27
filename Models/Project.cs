using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace bug_tracker.Models
{
    public class Project
    {
        public Project() {
            this.Tickets = new HashSet<Ticket>();
        }

        public int Id { get; set; }
        public System.DateTimeOffset Created { get; set; }
        public Nullable<System.DateTimeOffset> Updated { get; set; }
        public string Title { get; set; }
        public virtual ApplicationUser Manager { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}