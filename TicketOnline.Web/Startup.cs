using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TicketOnline.Web.Startup))]
namespace TicketOnline.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
