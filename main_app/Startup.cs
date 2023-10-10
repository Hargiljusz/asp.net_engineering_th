using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(main_app.Startup))]
namespace main_app
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
