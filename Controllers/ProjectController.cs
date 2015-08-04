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
        [Authorize(Roles="Admin,Project Manager,Developer")]
        public ActionResult Index()
        {
            //var managerRoleId = "c78c3f14-d4e4-4f19-8dc9-e0a8094cb1e1";
            //ViewBag.Managers = db.Users.Where(u => u.Roles.Any(r => r.RoleId == managerRoleId)).ToArray();
            if (User.IsInRole("Project Manager"))
            {
                return View(db.Projects.Where(u => u.Users.Any(m => m.Projects.Contains(u))).ToList());
            }
            if (User.IsInRole("Developer"))
            {
                return View(db.Projects.Where(u => u.Users.Any(d => d.Projects.Contains(u))).ToList());
            }
            return View(db.Projects.ToList());
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title")] Project project)
        {
            db.Projects.Add(project);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title")] Project project)
        {
            if (ModelState.IsValid)
            {
                db.Projects.Attach(project);
                db.Entry(project).Property("Title").IsModified = true;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}