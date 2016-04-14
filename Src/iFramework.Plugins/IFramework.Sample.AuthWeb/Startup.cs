using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(IFramework.Sample.AuthWeb.Startup))]
namespace IFramework.Sample.AuthWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
