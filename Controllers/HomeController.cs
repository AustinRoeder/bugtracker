using bug_tracker.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace bug_tracker.Controllers
{
    public class HomeController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();
        public ActionResult Index()
        {
            var user = User.Identity.GetUserId() != null ? db.Users.Find(User.Identity.GetUserId()) : null;
            var model = new List<TicketChartDetails>();
            if (user == null)
                return RedirectToAction("Login", "Account");
            ViewBag.NotInRole = db.Users.Where(u => u.Roles.Count == 0);
            if (User.IsInRole("Global Admin") || User.IsInRole("Admin"))
            {
                ViewBag.ProjectCount = db.Projects.Count();
                model = db.Projects.Select(p => new TicketChartDetails()
                {
                    Title = p.Title,
                    TicketCount = p.Tickets.Count(),
                    PercentCompleted = p.Tickets.Count() != 0 ? (double)p.Tickets.Where(t => t.Status.Name == "Completed").Count() / (double)p.Tickets.Count() * 100 : 100,
                }).ToList();
            }
            else
            {
                ViewBag.ProjectCount = user.Projects.Count();
                model = user.Projects.Select(p => new TicketChartDetails()
                {
                    Title = p.Title,
                    TicketCount = p.Tickets.Count(),
                    PercentCompleted = (double)p.Tickets.Where(t => t.Status.Name == "Completed").Count() / (double)p.Tickets.Count() * 100,
                }).ToList();
            }
            return View(model);
        }
        public ActionResult AddSub(string id)
        {
            Helpers.UserRolesHelper helper = new Helpers.UserRolesHelper();
            if(!String.IsNullOrWhiteSpace(id))
                helper.AddUserToRole(id, "Submitter");

            return RedirectToAction("Index", "Home");
        }
        public ActionResult GetChart()
        {
            var tix = db.Tickets.AsQueryable();
            var user = db.Users.Find(User.Identity.GetUserId());
            if (User.IsInRole("Admin") || User.IsInRole("Global Admin"))
                tix = db.Tickets;
            else if (User.IsInRole("Project Manager"))
                tix = tix.Where(t => t.Project.Users.Contains(user));
            else if (User.IsInRole("Developer"))
                tix = tix.Where(t => t.AssignedToUserId == user.Id);
            else
                tix = tix.Where(t => t.OwnerId == user.Id);
            var donut = (from s in db.TStatuses
                         let aCount = (from t in tix
                                       where s.Id == t.StatusId
                                       select t).Count()
                         select new
                         {
                             label = s.Name,
                             value = aCount
                         }).ToArray();
            var result = new
            {
                donut = donut,
            };

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }
    }
}