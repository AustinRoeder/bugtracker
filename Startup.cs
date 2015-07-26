using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(bug_tracker.Startup))]
namespace bug_tracker
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
