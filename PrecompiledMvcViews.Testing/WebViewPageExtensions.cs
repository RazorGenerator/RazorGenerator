using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;
using HtmlAgilityPack;
using ReflectionMagic;

namespace PrecompiledMvcViews.Testing {
    public static class WebViewPageExtensions {
        private static DummyViewEngine _viewEngine = new DummyViewEngine();

        public static string Render<TModel>(this WebViewPage<TModel> view, TModel model = default(TModel)) {
            view.Initialize();

            view.ViewData.Model = model;

            var webPageContext = new WebPageContext(view.ViewContext.HttpContext, page: null, model: null);
            var writer = new StringWriter();

            // Using private reflection to access some internals
            // Note: ideally we would not have to do this!
            view.AsDynamic().PageContext = webPageContext;
            webPageContext.AsDynamic().OutputStack.Push(writer);

            view.Execute();

            return writer.ToString();
        }

        public static HtmlDocument RenderAsHtml<TModel>(this WebViewPage<TModel> view, TModel model = default(TModel)) {
            string html = Render(view, model);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }

        private static void Initialize<TModel>(this WebViewPage<TModel> view) {
            EnsureViewEngineRegistered();

            var context = new DummyHttpContext();
            var routeData = new RouteData();

            var requestContext = new RequestContext(context, routeData);
            var controllerContext = new ControllerContext(context, routeData, new DummyController());

            view.ViewContext = new ViewContext(controllerContext, new DummyView(), view.ViewData, new TempDataDictionary(), new StringWriter());

            view.InitHelpers();
        }

        private static void EnsureViewEngineRegistered() {
            // Make sure our dummy view engine is registered
            lock (_viewEngine) {
                if (!ViewEngines.Engines.Contains(_viewEngine)) {
                    ViewEngines.Engines.Clear();
                    ViewEngines.Engines.Insert(0, _viewEngine);
                }
            }
        }

        // Define minimal 'dummy' implementation of various things needed in order to render the view

        class DummyViewEngine : IViewEngine {
            public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache) {
                return new ViewEngineResult(new DummyView { ViewName = partialViewName }, this);
            }

            public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache) {
                return new ViewEngineResult(new DummyView { ViewName = viewName }, this);
            }

            public void ReleaseView(ControllerContext controllerContext, IView view) {
            }
        }

        class DummyView : IView {
            public string ViewName { get; set; }

            public void Render(ViewContext viewContext, TextWriter writer) {
                // Render a marker instead of actually rendering the partial view
                writer.WriteLine(String.Format("/* {0} */", ViewName));
            }
        }

        class DummyController : ControllerBase {
            protected override void ExecuteCore() { }
        }


        class DummyHttpRequest : HttpRequestBase {
            public override bool IsLocal {
                get { return false; }
            }

            public override string ApplicationPath {
                get { return "/"; }
            }

            public override NameValueCollection ServerVariables {
                get { return new NameValueCollection(); }
            }

            public override string RawUrl {
                get { return ""; }
            }
        }

        class DummyHttpResponse : HttpResponseBase {
            public override string ApplyAppPathModifier(string virtualPath) {
                return virtualPath;
            }
        }

        class DummyHttpContext : HttpContextBase {
            private HttpRequestBase _request = new DummyHttpRequest();
            private HttpResponseBase _response = new DummyHttpResponse();
            private IDictionary _items = new Hashtable();

            public override HttpRequestBase Request {
                get { return _request; }
            }

            public override HttpResponseBase Response {
                get { return _response; }
            }

            public override IDictionary Items {
                get { return _items; }
            }
        }

    }
}
