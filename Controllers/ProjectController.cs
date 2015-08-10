using bug_tracker.Helpers;
using bug_tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace bug_tracker.Controllers
{
    public class ProjectController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();
        UserRolesHelper helper = new UserRolesHelper();

        // GET: Project
        [Authorize(Roles="Admin,Global Admin,Project Manager,Developer")]
        public ActionResult Index()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            if (User.IsInRole("Project Manager"))
            {
                return View(user.Projects.ToList());
            }
            if (User.IsInRole("Developer"))
            {
                return View(user.Projects.ToList());
            }
            return View(db.Projects.ToList());
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Global Admin,Project Manager")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title")] Project project)
        {
            db.Projects.Add(project);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Global Admin,Project Manager")]
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
        [Authorize(Roles="Admin,Global Admin,Project Manager")]
        public ActionResult Users(int? id)
        {
            var pThis = db.Projects.FirstOrDefault(p => p.Id == id);
            var subRoleId = db.Roles.FirstOrDefault(r=>r.Name == "Submitter").Id;
            var project = new ProjectManager
            {
                ProjectId = pThis.Id,
                ProjectName = pThis.Title,
                Users = new MultiSelectList(db.Users.Where(u=>!u.Roles.Any(r=>r.RoleId == subRoleId)).ToList(), "Id", "DisplayName", pThis.Users.Select(u => u.Id).ToList())
            };
            
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Users(int? id, string[] selected)
        {
            if (ModelState.IsValid)
            {
                var project = db.Projects.FirstOrDefault(r => r.Id == id);
                if (project == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                project.Users.Clear();
                foreach (var userId in selected ?? new string[0])
                {
                    var user = db.Users.Find(userId);
                    project.Users.Add(user);
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}