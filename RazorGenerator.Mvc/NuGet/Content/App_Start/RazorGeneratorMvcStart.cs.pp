using System.Web.Mvc;
using System.Web.WebPages;
using RazorGenerator.Mvc;

[assembly: WebActivator.PreApplicationStartMethod(typeof($rootnamespace$.App_Start.RazorGeneratorMvcStart), "Start")]

namespace $rootnamespace$.App_Start {
    public static class RazorGeneratorMvcStart {
        public static void Start() {
            var engine = new PrecompiledMvcEngine(typeof(RazorGeneratorMvcStart).Assembly);

            ViewEngines.Engines.Insert(0, engine);

            // StartPage lookups are done by WebPages. 
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);
        }
    }
}
