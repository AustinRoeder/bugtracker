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
            if (User.IsInRole("Global Admin") || User.IsInRole("Admin"))
                model = db.Projects.Select(p => new TicketChartDetails()
                {
                    Title = p.Title,
                    TicketCount = p.Tickets.Count(),
                    PercentCompleted = (double)p.Tickets.Where(t => t.Status.Name == "Completed").Count() / (double)p.Tickets.Count() * 100,
                    NotInRole = db.Users.Where(u => u.Roles.Count == 0),
                }).ToList();
            else
                model = user.Projects.Select(p => new TicketChartDetails()
                {
                    Title = p.Title,
                    TicketCount = p.Tickets.Count(),
                    PercentCompleted = (double)p.Tickets.Where(t => t.Status.Name == "Completed").Count() / (double)p.Tickets.Count() * 100,
                    NotInRole = null,
                }).ToList();

            return View(model);
        }
        public ActionResult GetChart()
        {
            var tTotal = db.Tickets.Count();
            var donut = (from s in db.TStatuses
                         let aCount = (from t in db.Tickets
                                       where s.Id == t.StatusId
                                       select t).Count()
                         select new
                         {
                             label = s.Name,
                             value = aCount
                         }).ToArray();
            var progress = (from p in db.Projects
                                let tCount = p.Tickets.Count
                                select new { 
                                    label = p.Title,
                                    value = tCount,
                                    percent = (tCount / (double)tTotal) * 100
                                }).ToArray();
            var result = new
            {
                donut = donut,
                progress = progress,
            };

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }
    }
}