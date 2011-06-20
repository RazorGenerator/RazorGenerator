using System.Web.WebPages;
using System.Web.WebPages.Razor;

namespace PrecompiledWebPagesHelper {
    public static class PreApplicationStartCode {
        private static bool _startCalled;

        public static void Start() {
            if (_startCalled) {
                return;
            }
            _startCalled = true;

            var type = typeof(PreApplicationStartCode);

            // This is to make the precompiled web site work
            ApplicationPart.Register(new ApplicationPart(type.Assembly, @"~/sample"));

            // This is to make the precompiled helper work without an import.
            WebPageRazorHost.AddGlobalImport(type.Namespace);
        }
    }
}
