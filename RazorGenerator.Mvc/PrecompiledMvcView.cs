using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using System.Web.WebPages;

namespace RazorGenerator.Mvc
{
    public class PrecompiledMvcView : IView
    {
        private readonly Type _type;
        private readonly string _virtualPath;

        public PrecompiledMvcView(string virtualPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension)
        {
            _type = type;
            _virtualPath = virtualPath;
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
    }
}
