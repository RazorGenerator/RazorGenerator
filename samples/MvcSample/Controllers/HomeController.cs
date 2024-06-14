using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using RazorGenerator.Mvc;

namespace MvcSample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult InAppPrecompiled()
        {
            return View();
        }

        public ActionResult ExternalPrecompiled()
        {
            return View();
        }

        public ActionResult HelperDemo()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Yield()
        {
            IEnumerable<int> model = YieldRows(); // using 'ToList' makes error go away .ToList();

            return View(model);
        }

        private IEnumerable<int> YieldRows()
        {
            for (int i = 0; i < 5; i++)
            {
                yield return i;
            }
        }

        [HttpPost]
        public ActionResult InAppPrecompiled(bool? executePrecompiledEngineFirst)
        {
            var engines = ViewEngines.Engines.OfType<PrecompiledMvcEngine>().ToArray();


            foreach (var engine in engines)
            {
                ViewEngines.Engines.Remove(engine);

                if (executePrecompiledEngineFirst ?? false)
                {
                    ViewEngines.Engines.Insert(0, engine);
                }
                else
                {
                    ViewEngines.Engines.Add(engine);
                }
            }

            return View();
        }

        public ActionResult UsingRoutes()
        {
            return View();
        }
    }
}
