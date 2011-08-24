using System.Web.Mvc;
using System.Web.WebPages;
using RazorGenerator.Mvc;

namespace PrecompiledMvcLibrary {

    public static class PreApplicationStartCode {
        private static bool _startMethodExecuted = false;

        public static void Start() {
            if (_startMethodExecuted == true) {
                return;
            }

            var engine = new PrecompiledMvcEngine(typeof(PreApplicationStartCode).Assembly);

            ViewEngines.Engines.Insert(0, engine);

            // StartPage lookups are done by WebPages. 
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);

            _startMethodExecuted = true;
        }
    }
}
