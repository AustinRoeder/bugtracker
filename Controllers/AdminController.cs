using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;

using bug_tracker.Models;
using bug_tracker.Helpers;

using Microsoft.AspNet.Identity.Owin;

namespace bug_tracker.Controllers
{
    [Authorize(Roles="Admin")]
    [RequireHttps]
    public class AdminController : Controller
    {
        UserRolesHelper helper = new UserRolesHelper();
        ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Users()
        {

            var roles = db.Roles.ToList();
            var users = db.Users.ToList();

            var model = new UserRoles
            {
                UserList = users,
                RoleList = roles,
                noRoles = db.Users.Where(u => u.Roles.All(r => r.UserId != u.Id)).ToList()
            };

            return View(model);


        }

        public ActionResult Edit(string RoleName)
        {
            var usersInRole = helper.UsersInRole(RoleName).Select(u => u.Id);

            var model = new RoleManager
            {
                RoleId = db.Roles.FirstOrDefault(r => r.Name == RoleName).Id,
                RoleName = RoleName,
                Users = new MultiSelectList(db.Users.ToList(), "Id", "DisplayName", usersInRole),
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string RoleName, string[] selected)
        {
            if (ModelState.IsValid)
            {
                var role = db.Roles.FirstOrDefault(r => r.Name == RoleName);
                if (role == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                role.Users.Clear();
                db.SaveChanges();
                foreach (var userId in selected ?? new string[0])
                {
                    helper.AddUserToRole(userId, RoleName);
                }
            }
            return RedirectToAction("Edit", new { RoleName });
        }
    }
}