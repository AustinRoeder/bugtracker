namespace bug_tracker.Migrations
{
    using bug_tracker.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Configuration;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<bug_tracker.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "bug_tracker.Models.ApplicationDbContext";
        }

        protected override void Seed(bug_tracker.Models.ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(
                new RoleStore<IdentityRole>(context));

            if (!context.Roles.Any(r => r.Name == "Admin"))
            {
                roleManager.Create(new IdentityRole { Name = "Admin" });
            }
            if (!context.Roles.Any(r => r.Name == "Project Manager"))
            {
                roleManager.Create(new IdentityRole { Name = "Project Manager" });
            }
            if (!context.Roles.Any(r => r.Name == "Developer"))
            {
                roleManager.Create(new IdentityRole { Name = "Developer" });
            }
            if (!context.Roles.Any(r => r.Name == "Submitter"))
            {
                roleManager.Create(new IdentityRole { Name = "Submitter" });
            }

            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));

            if (!context.Users.Any(u => u.Email == "austinjroeder@gmail.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "austinjroeder@gmail.com",
                    Email = "austinjroeder@gmail.com",
                    FirstName = "Austin",
                    LastName = "Roeder",
                    DisplayName = "Austin",
                }, "Purdue96!");
            }
            if (!context.Users.Any(u => u.Email == "demo@bugtracker.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "demo@bugtracker.com",
                    Email = "demo@bugtracker.com",
                    FirstName = "Demo",
                    LastName = "Admin",
                    DisplayName = "Demo",
                }, "Password1!");
            }

            var demoID = userManager.FindByEmail("demo@bugtracker.com").Id;
            userManager.AddToRole(demoID, "Admin");

            var userID = userManager.FindByEmail("austinjroeder@gmail.com").Id;
            userManager.AddToRole(userID, "Admin");
        }
    }
}
