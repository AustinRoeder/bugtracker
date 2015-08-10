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

        public ActionResult Index(string checkedVal)
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            ViewBag.Devs = helper.UsersInRole("Developer");
            ViewBag.Checked = checkedVal;
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
                if(ViewBag.Checked != null && ViewBag.Checked == "checked")
                {
                    return View(user.Projects.SelectMany(p => p.Tickets.Where(t=>t.AssignedToUserId == user.Id)).ToList());
                }
                return View(user.Projects.SelectMany(p=>p.Tickets).ToList());
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

            ticket.OwnerId = User.Identity.GetUserId();
            ticket.StatusId = 1;
            ticket.Created = DateTimeOffset.Now.LocalDateTime;
            db.Tickets.Add(ticket);
            db.SaveChanges();
            return RedirectToAction("Index");
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
        public async Task<ActionResult> Edit([Bind(Include = "Id,Title,Desc,StatusId,TypeId,PriorityId,ProjectId,AssignedToUserId")] Ticket ticket)
        {
            var oldTicket = db.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == ticket.Id);
            var Editor = db.Users.Find(User.Identity.GetUserId());
            var properties = new List<string>();
            //Checks for Ticket History
            if (oldTicket.Title != ticket.Title)
            {
                properties.Add("Title");
                var history = new THistory()
                {
                    TicketId = ticket.Id,
                    Property = "Title",
                    OldValue = oldTicket.Title,
                    NewValue = ticket.Title,
                    User = Editor,
                    Updated = DateTimeOffset.Now.LocalDateTime
                };
                db.THistories.Add(history);
            }
            if (oldTicket.Desc != ticket.Desc)
            {
                properties.Add("Desc");
                var history = new THistory()
                {
                    TicketId = ticket.Id,
                    Property = "Description",
                    OldValue = oldTicket.Desc,
                    NewValue = ticket.Desc,
                    User = Editor,
                    Updated = DateTimeOffset.Now.LocalDateTime
                };
                db.THistories.Add(history);
            }
            if (oldTicket.StatusId != ticket.StatusId)
            {
                properties.Add("StatusId");
                var history = new THistory()
                {
                    TicketId = ticket.Id,
                    Property = "Status",
                    OldValue = oldTicket.Status.Name,
                    NewValue = db.TStatuses.Find(ticket.StatusId).Name,
                    User = Editor,
                    Updated = DateTimeOffset.Now.LocalDateTime
                };
                db.THistories.Add(history);
            }
            if (oldTicket.TypeId != ticket.TypeId)
            {
                properties.Add("TypeId");
                var history = new THistory()
                {
                    TicketId = ticket.Id,
                    Property = "Type",
                    OldValue = oldTicket.Type.Name,
                    NewValue = db.TTypes.Find(ticket.TypeId).Name,
                    User = Editor,
                    Updated = DateTimeOffset.Now.LocalDateTime
                };
                db.THistories.Add(history);
            }
            if (oldTicket.PriorityId != ticket.PriorityId)
            {
                properties.Add("PriorityId");
                var history = new THistory()
                {
                    TicketId = ticket.Id,
                    Property = "Priority",
                    OldValue = oldTicket.Priority.Name,
                    NewValue = db.TPriorities.Find(ticket.PriorityId).Name,
                    User = Editor,
                    Updated = DateTimeOffset.Now.LocalDateTime
                };
                db.THistories.Add(history);
            }
            if (oldTicket.ProjectId != ticket.ProjectId)
            {
                properties.Add("ProjectId");
                var history = new THistory()
                {
                    TicketId = ticket.Id,
                    Property = "Project",
                    OldValue = oldTicket.Project.Title,
                    NewValue = db.Projects.Find(ticket.ProjectId).Title,
                    User = Editor,
                    Updated = DateTimeOffset.Now.LocalDateTime
                };
                db.THistories.Add(history);
            }
            if (User.IsInRole("Admin") || User.IsInRole("Global Admin") || User.IsInRole("Project Manager"))
            {
                if (oldTicket.AssignedToUserId != ticket.AssignedToUserId)
                {
                    properties.Add("AssignedToUserId");
                    var oldUsername = oldTicket.AssignedToUser != null ? oldTicket.AssignedToUser.DisplayName : "Unassigned";
                    var history = new THistory()
                    {
                        TicketId = ticket.Id,
                        Property = "Assigned User",
                        OldValue = oldUsername,
                        NewValue = db.Users.Find(ticket.AssignedToUserId).DisplayName,
                        User = Editor,
                        Updated = DateTimeOffset.Now.LocalDateTime
                    };
                    db.THistories.Add(history);
                }
            }

            ticket.Updated = DateTimeOffset.Now.LocalDateTime;
            db.Update(ticket,properties.ToArray());
            await db.SaveChangesAsync();
            return RedirectToAction("Details", new { id = ticket.Id});
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
            return View("Details", new { id = ticket.Id });
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
            if (!exts.Any(item => item == ext))
                ModelState.AddModelError("image", "Invalid Format");
            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    var filePath = "/Uploads/";
                    var absPath = Server.MapPath("~" + filePath);
                    attachment.FilePath = filePath;
                    attachment.TicketId = ticket.Id;
                    attachment.UserId = User.Identity.GetUserId();
                    attachment.FileUrl = filePath + file.FileName;
                    attachment.Created = DateTimeOffset.Now.LocalDateTime;
                    file.SaveAs(Path.Combine(absPath, file.FileName));
                    db.TAttachments.Add(attachment);
                    db.SaveChanges();
                }
            }
            //return RedirectToAction("Index");
            return RedirectToAction("Details", new { id = ticket.Id });
        }
    }
}