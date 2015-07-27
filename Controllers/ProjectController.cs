using bug_tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace bug_tracker.Controllers
{
    public class ProjectController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();
        // GET: Project
        public ActionResult Index()
        {
            return View(db.Projects.OrderByDescending(p => p.Created).ToList());
        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Created,Updated,Title")] Project project)
        {
            project.Created = DateTimeOffset.Now.LocalDateTime;

            db.Projects.Add(project);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}