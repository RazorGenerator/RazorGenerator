using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using RazorGenerator.Mvc;

namespace PrecompiledMvcLibrary
{

    public class PreApplicationStartCode
    {
        private static bool _startMethodExecuted = false;

        public static void Start()
        {
            if (_startMethodExecuted == true)
            {
                return;
            }

            var engine = new PrecompiledMvcEngine(typeof(PreApplicationStartCode).Assembly);

            // If you have multiple assemblies with precompiled views,
            // use CompositePrecompiledMvcEngine:
            //
            //var engine = new CompositePrecompiledMvcEngine(
            //    PrecompiledViewAssembly.OfType<YourType>(),
            //    PrecompiledViewAssembly.OfType<PreApplicationStartCode>(
            //        usePhysicalViewsIfNewer: HttpContext.Current.IsDebuggingEnabled));

            ViewEngines.Engines.Insert(0, engine);

            // StartPage lookups are done by WebPages. 
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);

            _startMethodExecuted = true;
        }
    }
}
