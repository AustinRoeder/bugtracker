using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace bug_tracker.Models
{
    public class ProjectManager
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        public MultiSelectList AddedUsers { get; set; }
        public MultiSelectList DeletedUsers { get; set; }
        public MultiSelectList Users { get; set; }

        public string[] AddedSelect { get; set; }
        public string[] DeletedSelect { get; set; }
        public string[] Selected { get; set; }
    }
}