using System.Web.Mvc;

namespace PrecompiledMvcLibrary.Areas.MyPluggableArea.Controllers
{
    public class FooController : Controller
    {
        //
        // GET: /MyPluggableArea/Foo/

        public ActionResult Index()
        {
            return View();
        }
    }
}
