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
        [Authorize(Roles = "Admin,Global Admin,Project Manager,Developer")]
        public ActionResult Details(int id)
        {
            ViewBag.Project = db.Projects.Find(id);
            return View(db.Tickets.Where(t=>t.ProjectId == id).ToList());
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Global Admin,Project Manager")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title")] Project project)
        {
            db.Projects.Add(project);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = project.Id});
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
            return RedirectToAction("Details", new { id = project.Id});
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

        public ActionResult CreateTicket(int? id)
        {
            ViewBag.Projects = db.Projects.ToList();
            ViewBag.Project = db.Projects.Find(id);

            ViewBag.Devs = helper.UsersInRole("Developer").ToList();

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTicket([Bind(Include = "Id,Desc,OwnerId,ProjectId,StatusId,TypeId,PriorityId,Title,AssignedToUserId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.OwnerId = User.Identity.GetUserId();
                ticket.StatusId = 1;
                ticket.Created = DateTimeOffset.Now.LocalDateTime;
                db.Tickets.Add(ticket);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = ticket.ProjectId});
            }
            return View(ticket);
        }

        public ActionResult EditTicket(int? id)
        {
            ViewBag.Devs = helper.UsersInRole("Developer").ToList();
            var user = db.Users.Find(User.Identity.GetUserId());
            ViewBag.Projects = db.Projects.ToList();
            var model = db.Tickets.Find(id);
            if (User.IsInRole("Admin") || User.IsInRole("Global Admin"))
            {
                return View(model);
            }
            else if (User.IsInRole("Project Manager"))
            {
                if (user.Projects.Any(p => p.Tickets.Contains(model)))
                    return View(model);
            }
            else if (User.IsInRole("Developer"))
            {
                if (model.AssignedToUserId == user.Id)
                    return View(model);
            }
            return RedirectToAction("Details", new { id = model.ProjectId});

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTicket([Bind(Include = "Id,Title,Desc,Updated,ProjectId,AssignedToUserId,TypeId,PriorityId,StatusId")] Ticket ticket)
        {
            var editable = new List<string>() { "Title", "Desc" };
            if (User.IsInRole("Admin") || User.IsInRole("Global Admin"))
                editable.AddRange(new string[] { "ProjectId", "AssignedToUserId", "TypeId", "PriorityId", "StatusId" });
            if (User.IsInRole("Project Manager"))
                editable.AddRange(new string[] { "AssignedToUserId", "TypeId", "PriorityId", "StatusId" });

            if (ModelState.IsValid)
            {
                var oldTicket = db.Tickets.AsNoTracking()
                    .FirstOrDefault(t => t.Id == ticket.Id);
                var histories = GetTHistories(oldTicket, ticket)
                    .Where(h => editable.Contains(h.History.Property));

                var mailer = new EmailService();

                foreach (var item in histories)
                {
                    db.THistories.Add(item.History);
                    if (item.Notification != null)
                        mailer.SendAsync(item.Notification);
                }

                db.Update(ticket, editable.ToArray());
                db.SaveChanges();

                return RedirectToAction("Details", new { id = ticket.ProjectId});
            }
            return View(ticket);
        }
        private class THistoryWithNotification
        {
            public THistory History { get; set; }
            public IdentityMessage Notification { get; set; }
        }
        private List<THistoryWithNotification> GetTHistories(Ticket oldTicket, Ticket newTicket)
        {
            var histories = new List<THistoryWithNotification>();
            var newUser = db.Users.Find(newTicket.AssignedToUserId);

            if (oldTicket.AssignedToUserId != newTicket.AssignedToUserId)
            {
                var oldUser = oldTicket.AssignedToUserId != null ? oldTicket.AssignedToUser.UserName : "Unassigned";
                histories.Add(new THistoryWithNotification()
                {
                    History = new THistory()
                    {
                        TicketId = newTicket.Id,
                        UserId = newUser.Id,
                        Property = "AssignedToUserId",
                        PropertyDisplay = "Assigned User",
                        OldValue = oldTicket.AssignedToUserId,
                        OldValueDisplay = oldUser,
                        NewValue = newUser.Id,
                        NewValueDisplay = newUser.UserName
                    },
                    Notification = newUser != null ? new IdentityMessage()
                    {
                        Subject = "You have a new Notification",
                        Destination = newUser.Email,
                        Body = "You have been assigned to a new ticket with Id " + newTicket.Id + "!"
                    } : null
                });
            }
            if (oldTicket.Desc != newTicket.Desc)
                histories.Add(new THistoryWithNotification()
                {
                    History = new THistory()
                    {
                        TicketId = newTicket.Id,
                        UserId = newUser.Id,
                        Property = "Desc",
                        PropertyDisplay = "Description",
                        OldValue = oldTicket.Desc,
                        OldValueDisplay = oldTicket.Desc,
                        NewValue = newTicket.Desc,
                        NewValueDisplay = newTicket.Desc
                    },
                    Notification = null
                });
            if (oldTicket.Title != newTicket.Title)
                histories.Add(new THistoryWithNotification()
                {
                    History = new THistory()
                    {
                        TicketId = newTicket.Id,
                        UserId = newUser.Id,
                        Property = "Title",
                        PropertyDisplay = "Title",
                        OldValue = oldTicket.Title,
                        OldValueDisplay = oldTicket.Title,
                        NewValue = newTicket.Title,
                        NewValueDisplay = newTicket.Title
                    },
                    Notification = null
                });
            if (oldTicket.PriorityId != newTicket.PriorityId)
                histories.Add(new THistoryWithNotification()
                {
                    History = new THistory()
                    {
                        TicketId = newTicket.Id,
                        UserId = newUser.Id,
                        Property = "PriorityId",
                        PropertyDisplay = "Priority",
                        OldValue = oldTicket.PriorityId.ToString(),
                        OldValueDisplay = oldTicket.Priority.Name,
                        NewValue = newTicket.PriorityId.ToString(),
                        NewValueDisplay = db.TPriorities.Find(newTicket.PriorityId).Name
                    },
                    Notification = null
                });
            if (oldTicket.StatusId != newTicket.StatusId)
                histories.Add(new THistoryWithNotification()
                {
                    History = new THistory()
                    {
                        TicketId = newTicket.Id,
                        UserId = newUser.Id,
                        Property = "StatusId",
                        PropertyDisplay = "Status",
                        OldValue = oldTicket.StatusId.ToString(),
                        OldValueDisplay = oldTicket.Status.Name,
                        NewValue = newTicket.StatusId.ToString(),
                        NewValueDisplay = db.TStatuses.Find(newTicket.StatusId).Name
                    },
                    Notification = null
                });
            if (oldTicket.TypeId != newTicket.TypeId)
                histories.Add(new THistoryWithNotification()
                {
                    History = new THistory()
                    {
                        TicketId = newTicket.Id,
                        UserId = newUser.Id,
                        Property = "TypeId",
                        PropertyDisplay = "Type",
                        OldValue = oldTicket.TypeId.ToString(),
                        OldValueDisplay = oldTicket.Type.Name,
                        NewValue = newTicket.TypeId.ToString(),
                        NewValueDisplay = db.TTypes.Find(newTicket.TypeId).Name
                    },
                    Notification = null
                });
            if (oldTicket.ProjectId != newTicket.ProjectId)
            {
                var newProject = db.Projects.Find(newTicket.ProjectId);
                histories.Add(new THistoryWithNotification()
                {
                    History = new THistory()
                    {
                        TicketId = newTicket.Id,
                        UserId = newUser.Id,
                        Property = "ProjectId",
                        PropertyDisplay = "Project",
                        OldValue = oldTicket.ProjectId.ToString(),
                        OldValueDisplay = oldTicket.Project.Title,
                        NewValue = newTicket.ProjectId.ToString(),
                        NewValueDisplay = db.Projects.Find(newTicket.ProjectId).Title
                    },
                    Notification = newProject != null ? new IdentityMessage()
                    {
                        Subject = "You have a new Notification",
                        Destination = newUser.Email,
                        Body = "A ticket assigned to you has been moved to a different project" +
                                        " of Id " + newProject.Id + "!"
                    } : null
                });
            }

            return histories;
        }

    }
}