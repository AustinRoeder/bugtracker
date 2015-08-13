using System;
using System.Linq;
using System.Web;
using System.Security.Claims;
using System.Security.Principal;
using System.Collections.Generic;

using Microsoft.AspNet.Identity;

namespace bug_tracker.Helpers
{
    public static class CustomExtensions
    {
        public static string GetName(this IIdentity user)
        {
            var ClaimUser = (ClaimsIdentity)user;
            var nameClaim = ClaimUser.Claims.FirstOrDefault(c => c.Type == "DisplayName");
            if (nameClaim != null)
            {
                return nameClaim.Value;
            }
            else
            {
                return null;
            }
        }
    }
}