using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Web.PrecompiledMvcView;

namespace PrecompiledMvcSample.Controllers {
    public class HomeController : Controller {
        public ActionResult Index() {
            ViewBag.Message = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult InAppPrecompiled() {
            return View();
        }

        public ActionResult ExternalPrecompiled() {
            return View();
        }

        public ActionResult About() {
            return View();
        }

        [HttpPost]
        public ActionResult InAppPrecompiled(bool? executePrecompiledEngineFirst) {
            var engines = ViewEngines.Engines.OfType<PrecompiledMvcEngine>().ToArray();

            
            foreach(var engine in engines) {
                ViewEngines.Engines.Remove(engine);

                if (executePrecompiledEngineFirst ?? false) {
                    ViewEngines.Engines.Insert(0, engine);
                }
                else {
                    ViewEngines.Engines.Add(engine);
                }
            }

            return View();
        }
    }
}
