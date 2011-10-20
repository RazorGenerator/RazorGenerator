using System.Web.Mvc;

namespace PrecompiledMvcLibrary.Areas.MyPluggableArea
{
    public class MyPluggableAreaAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "MyPluggableArea";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "MyPluggableArea_default",
                "MyPluggableArea/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
