using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;

namespace RazorGenerator.Testing
{
    public static class WebViewPageExtensions
    {
        private static readonly DummyViewEngine _viewEngine = new DummyViewEngine();

        public static string Render<TModel>(this WebViewPage<TModel> view, TModel model = default(TModel))
        {
            return Render<TModel>(view, null, model);
        }

        public static string Render<TModel>(this WebViewPage<TModel> view, HttpContextBase httpContext, TModel model = default(TModel))
        {
            view.Initialize(httpContext);

            view.ViewData.Model = model;

            var webPageContext = new WebPageContext(view.ViewContext.HttpContext, page: null, model: null);
            var writer = new StringWriter();

            // Using private reflection to access some internals
            // Note: ideally we would not have to do this, but WebPages is just not mockable enough :(

            var dynamicView = view.AsDynamic();
            dynamicView.PageContext = webPageContext;
            webPageContext.AsDynamic().OutputStack.Push(writer);

            // Push some section writer dictionary onto the stack. We need two, because the logic in WebPageBase.RenderBody
            // checks that as a way to make sure the layout page is not called directly
            var sectionWriters = new Dictionary<string, SectionWriter>(StringComparer.OrdinalIgnoreCase);
            dynamicView.SectionWritersStack.Push(sectionWriters);
            dynamicView.SectionWritersStack.Push(sectionWriters);

            // Set the body delegate to do nothing
            dynamicView._body = (Action<TextWriter>)(w => { });

            view.Execute();

            return writer.ToString();
        }

        public static HtmlDocument RenderAsHtml<TModel>(this WebViewPage<TModel> view, TModel model = default(TModel))
        {
            return RenderAsHtml<TModel>(view, null, model);
        }

        public static HtmlDocument RenderAsHtml<TModel>(this WebViewPage<TModel> view, HttpContextBase httpContext, TModel model = default(TModel))
        {
            string html = Render(view, model);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }

        private static void Initialize<TModel>(this WebViewPage<TModel> view, HttpContextBase httpContext)
        {
            EnsureViewEngineRegistered();

            var context = httpContext ?? CreateMockContext();
            var routeData = new RouteData();

            var requestContext = new RequestContext(context, routeData);
            var controllerContext = new ControllerContext(context, routeData, new DummyController());

            view.ViewContext = new ViewContext(controllerContext, new DummyView(), view.ViewData, new TempDataDictionary(), new StringWriter());

            view.InitHelpers();
        }

        private static void EnsureViewEngineRegistered()
        {
            // Make sure our dummy view engine is registered
            lock (_viewEngine)
            {
                if (!ViewEngines.Engines.Contains(_viewEngine))
                {
                    ViewEngines.Engines.Clear();
                    ViewEngines.Engines.Insert(0, _viewEngine);
                }
            }
        }

        /// <summary>
        /// Creates a basic HttpContext mock for rendering a view.
        /// </summary>
        /// <returns>A mocked HttpContext object</returns>
        private static HttpContextBase CreateMockContext()
        {
            // Use Moq for faking context objects as it can setup all members
            // so that by default, calls to the members return a default/null value 
            // instead of a not implemented exception.

            // members were we want specific values returns are setup explicitly.

            // mock the request object
            var mockRequest = new Mock<HttpRequestBase>(MockBehavior.Loose);
            mockRequest.Setup(m => m.IsLocal).Returns(false);
            mockRequest.Setup(m => m.ApplicationPath).Returns("/");
            mockRequest.Setup(m => m.ServerVariables).Returns(new NameValueCollection());
            mockRequest.Setup(m => m.RawUrl).Returns(string.Empty);
            mockRequest.Setup(m => m.Cookies).Returns(new HttpCookieCollection());

            // mock the response object
            var mockResponse = new Mock<HttpResponseBase>(MockBehavior.Loose);
            mockResponse.Setup(m => m.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>((virtualPath) => virtualPath);
            mockResponse.Setup(m => m.Cookies).Returns(new HttpCookieCollection());

            // mock the httpcontext

            var mockHttpContext = new Mock<HttpContextBase>(MockBehavior.Loose);
            mockHttpContext.Setup(m => m.Items).Returns(new Hashtable());
            mockHttpContext.Setup(m => m.Request).Returns(mockRequest.Object);
            mockHttpContext.Setup(m => m.Response).Returns(mockResponse.Object);

            return mockHttpContext.Object;
        }

        // Define minimal 'dummy' implementation of various things needed in order to render the view

        class DummyController : ControllerBase
        {
            // Moq can't help with protected members.
            protected override void ExecuteCore() { }
        }

        class DummyViewEngine : IViewEngine
        {
            public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
            {
                return new ViewEngineResult(new DummyView { ViewName = partialViewName }, this);
            }

            public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
            {
                return new ViewEngineResult(new DummyView { ViewName = viewName }, this);
            }

            public void ReleaseView(ControllerContext controllerContext, IView view)
            {
            }
        }

        class DummyView : IView
        {
            public string ViewName { get; set; }

            public void Render(ViewContext viewContext, TextWriter writer)
            {
                // Render a marker instead of actually rendering the partial view
                writer.WriteLine(String.Format("/* {0} */", ViewName));
            }
        }
    }
}
