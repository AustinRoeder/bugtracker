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
            if (!context.Users.Any(u => u.Email == "admin@bugtracker.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "admin@bugtracker.com",
                    Email = "admin@bugtracker.com",
                    FirstName = "Admin",
                    LastName = "Demo",
                    DisplayName = "Admin Demo",
                }, "Password1!");
            }

            if (!context.Users.Any(u => u.Email == "manager@bugtracker.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "manager@bugtracker.com",
                    Email = "manager@bugtracker.com",
                    FirstName = "Manager",
                    LastName = "Demo",
                    DisplayName = "Manager Demo",
                }, "Password1!");
            }

            if (!context.Users.Any(u => u.Email == "dev@bugtracker.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "dev@bugtracker.com",
                    Email = "dev@bugtracker.com",
                    FirstName = "Developer",
                    LastName = "Demo",
                    DisplayName = "Developer Demo",
                }, "Password1!");
            }

            if (!context.Users.Any(u => u.Email == "sub@bugtracker.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "sub@bugtracker.com",
                    Email = "sub@bugtracker.com",
                    FirstName = "Submitter",
                    LastName = "Demo",
                    DisplayName = "Submitter Demo",
                }, "Password1!");
            }

            var subID = userManager.FindByEmail("sub@bugtracker.com").Id;
            userManager.AddToRole(subID, "Submitter");

            var devID = userManager.FindByEmail("dev@bugtracker.com").Id;
            userManager.AddToRole(devID, "Developer");

            var modID = userManager.FindByEmail("manager@bugtracker.com").Id;
            userManager.AddToRole(modID, "Project Manager");

            var demoID = userManager.FindByEmail("admin@bugtracker.com").Id;
            userManager.AddToRole(demoID, "Admin");

            var userID = userManager.FindByEmail("austinjroeder@gmail.com").Id;
            userManager.AddToRole(userID, "Admin");
        }
    }
}
