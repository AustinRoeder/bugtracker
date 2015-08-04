using bug_tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using bug_tracker.Helpers;

namespace bug_tracker.Controllers
{
    public class TicketController : Controller
    {
        // GET: Ticket
        ApplicationDbContext db = new ApplicationDbContext();
        
        public ActionResult Index()
        {
            return View(db.Tickets.OrderByDescending(t => t.Created).ToList());
        }
        [Authorize]
        public ActionResult Create()
        {
            ViewBag.Projects = db.Projects.ToList();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Desc,OwnerId,ProjectId,StatusId,TypeId,PriorityId,Title")] Ticket ticket)
        {
            ticket.OwnerId = User.Identity.GetUserId();
            ticket.StatusId = 1;
            ticket.Created = DateTimeOffset.Now.LocalDateTime;
            db.Tickets.Add(ticket);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        [Authorize]
        public ActionResult Edit(int? id)
        {
            ViewBag.Projects = db.Projects.ToList();
            
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Desc,StatusId,TypeId,PriorityId")] Ticket ticket)
        {
            ticket.StatusId = 2;
            ticket.Updated = DateTimeOffset.Now.LocalDateTime;
            db.Update(ticket, "Desc", "StatusId", "TypeId", "PriorityId", "Updated");
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}