using bug_tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using bug_tracker.Helpers;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace bug_tracker.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        // GET: Ticket
        ApplicationDbContext db = new ApplicationDbContext();
        UserRolesHelper helper = new UserRolesHelper();

        public ActionResult Index()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            ViewBag.Devs = helper.UsersInRole("Developer");
            if (User.IsInRole("Admin") || User.IsInRole("Global Admin"))
            {
                return View(db.Tickets.ToList());
            }
            else if (User.IsInRole("Project Manager"))
            {
                return View(user.Projects.SelectMany(p => p.Tickets).ToList());
            }
            else if (User.IsInRole("Developer"))
            {
                var dbTickets = db.Tickets.Where(t => t.AssignedToUserId == user.Id).ToList();
                List<Ticket> tickets = user.Projects.SelectMany(p=>p.Tickets).ToList();
                foreach (var t in dbTickets)
                {
                    if (!tickets.Contains(t))
                    {
                        tickets.Add(t);
                    }
                }
                
                return View(tickets);
            }
            else
            {
                return View(db.Tickets.Where(t=>t.OwnerId == user.Id).ToList());
            }
        }
        public ActionResult Create()
        {
            ViewBag.Projects = db.Projects.ToList();
            ViewBag.Devs = helper.UsersInRole("Developer").ToList();

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Desc,OwnerId,ProjectId,StatusId,TypeId,PriorityId,Title,AssignedToUserId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.OwnerId = User.Identity.GetUserId();
                ticket.StatusId = 1;
                ticket.Created = DateTimeOffset.Now.LocalDateTime;
                db.Tickets.Add(ticket);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(ticket);
        }
        public ActionResult Edit(int? id)
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
                if(user.Projects.Any(p=>p.Tickets.Contains(model)))
                    return View(model);
            }
            else if (User.IsInRole("Developer"))
            {
                if (model.AssignedToUserId == user.Id)
                    return View(model);
            }
            return RedirectToAction("Index");
            
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Desc,Created,Updated,ProjectId,AssignedToUserId,TypeId,PriorityId,StatusId")] Ticket ticket)
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

                return RedirectToAction("Index");
            }
            return View(ticket);
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
            if(oldTicket.Desc != newTicket.Desc)
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
            if(oldTicket.Title != newTicket.Title)
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
            if(oldTicket.PriorityId != newTicket.PriorityId)
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
            if(oldTicket.StatusId != newTicket.StatusId)
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

        private class THistoryWithNotification
        {
            public THistory History { get; set; }
            public IdentityMessage Notification { get; set; }
        }
        public ActionResult Details(int? id)
        {
            ViewBag.Devs = helper.UsersInRole("Developer");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }
        [HttpPost]
        [Authorize]
        public ActionResult CommentCreate(TComment comment)
        {
            Ticket ticket = db.Tickets.Find(comment.TicketId);
            if (ModelState.IsValid)
            {
                comment.OwnerId = User.Identity.GetUserId();
                comment.Created = DateTimeOffset.Now;
                db.TComments.Add(comment);
                db.SaveChanges();
                var ticketUrl = Url.Action("Details", "Ticket", new { id = comment.TicketId }, protocol: Request.Url.Scheme);
                var mailer = new EmailService();
                mailer.SendAsync(new IdentityMessage
                {
                    Subject = "You have a new Notification",
                    Destination = comment.Ticket.AssignedToUser.Email,
                    Body = "Your ticket," + comment.Ticket.Title + ", has a new comment! Visit the ticket <a href=\"" + ticketUrl + "\">here</a>"                    
                });
                return RedirectToAction("Details", new { id = ticket.Id });
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CommentEdit([Bind(Include = "Id,Body,TicketId")] TComment comment)
        {
            Ticket ticket = db.Tickets.Find(comment.TicketId);
            if (ModelState.IsValid)
            {
                db.TComments.Attach(comment);
                db.Update(comment, "Body");
                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id = ticket.Id });
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AttachmentCreate(HttpPostedFileBase file, int? id)
        {
            var ticket = db.Tickets.Find(id);
            var attachment = new TAttachment();
            var exts = new List<string>() { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".docx", ".txt", ".rtf", ".mac", ".pdf" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            var name = file.FileName.Split(ext.ToArray()).First();
            if (!exts.Any(item => item == ext))
                ModelState.AddModelError("image", "Invalid Format");
            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    var filePath = "/Upload/";
                    var absPath = Server.MapPath("~" + filePath);
                    Directory.CreateDirectory(absPath);
                    attachment.FilePath = filePath;
                    attachment.TicketId = ticket.Id;
                    attachment.FileExt = ext;
                    attachment.FileName = name;
                    attachment.UserId = User.Identity.GetUserId();
                    attachment.FileUrl = filePath + file.FileName;
                    attachment.Created = DateTimeOffset.Now.LocalDateTime;
                    file.SaveAs(Path.Combine(absPath, file.FileName));
                    db.TAttachments.Add(attachment);
                    db.SaveChanges();
                }
                var ticketUrl = Url.Action("Details", "Ticket", new { id = attachment.TicketId }, protocol: Request.Url.Scheme);
                var mailer = new EmailService();
                mailer.SendAsync(new IdentityMessage
                {
                    Subject = "You have a new Notification",
                    Destination = attachment.Ticket.AssignedToUser.Email,
                    Body = "Your ticket," + attachment.Ticket.Title + ", has a new attachment! Visit the ticket <a href=\"" + ticketUrl + "\">here</a>"
                });
            }
            return RedirectToAction("Details", new { id = ticket.Id });
        }
    }
}