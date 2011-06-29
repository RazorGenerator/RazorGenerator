using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PrecompiledMvcLibrary.Areas.MyPluggableArea.Controllers {
    public class FooController : Controller {
        //
        // GET: /MyPluggableArea/Foo/

        public ActionResult Index() {
            return View();
        }
    }
}
