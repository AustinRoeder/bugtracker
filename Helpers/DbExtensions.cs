using bug_tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace bug_tracker.Helpers
{
    public static class DbExtensions
    {
        public static bool Update<T>(this ApplicationDbContext context, T item, params string[] changedPropertyNames) where T : class, new()
        {
            context.Set<T>().Attach(item);
            foreach (var propertyName in changedPropertyNames)
            {
                // If we can't find the property, this line will throw an exception, 
                //which is good as we want to know about it
                context.Entry(item).Property(propertyName).IsModified = true;
            }
            return true;
        }
    }
}