using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using System.Web.WebPages;

namespace RazorGenerator.Mvc
{
    public class PrecompiledMvcView : IView
    {
        private readonly Type _type;
        private readonly string _virtualPath;
        private readonly string _masterPath;

        private delegate void OverriddenLayoutSetter(WebViewPage page, string layoutPath);
        private static readonly OverriddenLayoutSetter _overriddenLayoutSetter = CreateSetOverriddenLayoutDelegate();

        public PrecompiledMvcView(string virtualPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension)
            : this(virtualPath, null, type, runViewStartPages, fileExtension)
        {
        }
        
        public PrecompiledMvcView(string virtualPath, string masterPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension)
        {
            _type = type;
            _virtualPath = virtualPath;
            _masterPath = masterPath;
            RunViewStartPages = runViewStartPages;
            ViewStartFileExtensions = fileExtension;
        }

        public bool RunViewStartPages
        {
            get;
            private set;
        }

        public IEnumerable<string> ViewStartFileExtensions
        {
            get;
            private set;
        }

        public void Render(ViewContext viewContext, TextWriter writer)
        {
            object instance = Activator.CreateInstance(_type);

            WebViewPage webViewPage = instance as WebViewPage;

            if (webViewPage == null)
            {
                throw new InvalidOperationException("Invalid view type");
            }

            if (!String.IsNullOrEmpty(_masterPath))
            {
                _overriddenLayoutSetter(webViewPage, _masterPath);
            }
            webViewPage.VirtualPath = _virtualPath;
            webViewPage.ViewContext = viewContext;
            webViewPage.ViewData = viewContext.ViewData;
            webViewPage.InitHelpers();

            WebPageRenderingBase startPage = null;
            if (this.RunViewStartPages)
            {
                startPage = StartPage.GetStartPage(webViewPage, "_ViewStart", ViewStartFileExtensions);
            }

            var pageContext = new WebPageContext(viewContext.HttpContext, webViewPage, null);
            webViewPage.ExecutePageHierarchy(pageContext, writer, startPage);
        }

        // Unfortunately, the only way to override the default layout with a custom layout from a
        // ViewResult, without introducing a new subclass, is by setting the WebViewPage internal
        // property OverridenLayoutPath [sic].
        // This method makes use of reflection for creating a property setter in the form of a
        // delegate. The latter is used to improve performance, compared to invoking the MethodInfo
        // instance directly, without sacrificing maintainability.
        private static OverriddenLayoutSetter CreateSetOverriddenLayoutDelegate()
        {
            PropertyInfo property = typeof(WebViewPage).GetProperty("OverridenLayoutPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property == null)
            {
                throw new NotSupportedException("The WebViewPage internal property \"OverridenLayoutPath\" does not exist, probably due to an unsupported run-time version.");
            }

            MethodInfo setter = property.GetSetMethod(true);
            if (setter == null)
            {
                throw new NotSupportedException("The WebViewPage internal property \"OverridenLayoutPath\" exists but is missing a set method, probably due to an unsupported run-time version.");
            }

            return (OverriddenLayoutSetter)Delegate.CreateDelegate(typeof(OverriddenLayoutSetter), setter, true);
        }
    }
}
