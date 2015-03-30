using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MsBuildTest.Startup))]
namespace MsBuildTest
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
