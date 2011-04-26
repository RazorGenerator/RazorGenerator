using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Microsoft.Web.PrecompiledMvcView;
using System.Web.Mvc;
using System.Web.WebPages;

namespace PrecompiledMvcLibrary {

    public static class PreApplicationStartCode {
        private static bool _startMethodExecuted = false;

        public static void Start() {
            if (_startMethodExecuted == true) {
                return;
            }

            var engine = new PrecompiledMvcEngine(typeof(PreApplicationStartCode).Assembly);

            ViewEngines.Engines.Add(engine);

            // StartPage lookups are done by WebPages. 
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);

            _startMethodExecuted = true;
        }


    }
}
