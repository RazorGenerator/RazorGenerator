using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;
using HtmlAgilityPack;
using Moq;
using ReflectionMagic;

namespace RazorGenerator.Testing
{
    public static class WebViewPageExtensions
    {
        private static readonly object _lockObject = new object();

        private static readonly DummyViewEngine _viewEngine = new DummyViewEngine();

        private static readonly Dictionary<string, RouteValueDictionary> _urls = new Dictionary<string, RouteValueDictionary>();

        private static readonly Dictionary<string, ViewPlaceholder> _views = new Dictionary<string, ViewPlaceholder>();

        public static string Render<TModel>(this WebViewPage<TModel> view, TModel model = default(TModel))
        {
            return Render<TModel>(view, null, model);
        }

        public static string Render<TModel>(this WebViewPage<TModel> view, HttpContextBase httpContext, TModel model = default(TModel))
        {
            var writer = new StringWriter();
            view.Initialize(httpContext, writer);

            view.ViewData.Model = model;

            var webPageContext = new WebPageContext(view.ViewContext.HttpContext, page: null, model: null);

            // Using private reflection to access some internals
            // Note: ideally we would not have to do this, but WebPages is just not mockable enough :(
            var dynamicPageContext = webPageContext.AsDynamic();
            dynamicPageContext.OutputStack.Push(writer);

            // Push some section writer dictionary onto the stack. We need two, because the logic in WebPageBase.RenderBody
            // checks that as a way to make sure the layout page is not called directly
            var sectionWriters = new Dictionary<string, SectionWriter>(StringComparer.OrdinalIgnoreCase);
            dynamicPageContext.SectionWritersStack.Push(sectionWriters);
            dynamicPageContext.SectionWritersStack.Push(sectionWriters);

            // Set the body delegate to do nothing
            dynamicPageContext.BodyAction = (Action<TextWriter>)(w => { });

            view.AsDynamic().PageContext = webPageContext;
            view.Execute();

            return writer.ToString();
        }

        public static HtmlDocument RenderAsHtml<TModel>(this WebViewPage<TModel> view, TModel model = default(TModel))
        {
            return RenderAsHtml<TModel>(view, null, model);
        }

        public static HtmlDocument RenderAsHtml<TModel>(this WebViewPage<TModel> view, HttpContextBase httpContext, TModel model = default(TModel))
        {
            string html = Render(view, httpContext, model);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            PostProcessActionUrlNodes(doc);
            PostProcessPlaceholderNodes(doc);

            return doc;
        }

        private static void Initialize<TModel>(this WebViewPage<TModel> view, HttpContextBase httpContext, TextWriter writer)
        {
            EnsureDummyViewEngineRegistered();

            var context = httpContext ?? new HttpContextBuilder().Build();
            var routeData = new RouteData();

            var controllerContext = new ControllerContext(context, routeData, new Mock<ControllerBase>().Object);

            view.ViewContext = new ViewContext(controllerContext, new Mock<IView>().Object, view.ViewData, new TempDataDictionary(), writer);

            view.InitHelpers();
        }

        /// <summary>
        ///   Replaces placeholders for partial views with strongly-typed ViewNodes that can be checked by tests
        /// </summary>
        /// <param name="doc">The HtmlDocument rendered by the view</param>
        private static void PostProcessPlaceholderNodes(HtmlDocument doc)
        {
            foreach (var placeholder in doc.DocumentNode.Descendants("placeholder").ToList())
            {
                if (_views.ContainsKey(placeholder.InnerText))
                {
                    placeholder.ReplaceWith(_views[placeholder.InnerText].CreateViewNode(placeholder.OwnerDocument));
                    _views.Remove(placeholder.InnerText);
                }
            }
        }

        /// <summary>
        ///   Replaces nodes containing ActionUrls with strongly-typed ActionUrlNodes that can be checked by tests
        /// </summary>
        /// <param name="doc"></param>
        private static void PostProcessActionUrlNodes(HtmlDocument doc)
        {
            var guidLength = Guid.Empty.ToString().Length;

            var actionUrlAttributes =
              doc.DocumentNode.Descendants().SelectMany(n => n.Attributes.Where(a => a.Value.Length >= guidLength && _urls.ContainsKey(a.Value.Substring(a.Value.Length - guidLength)))).ToList();

            foreach (HtmlAttribute actionUrlAttribute in actionUrlAttributes)
            {
                var key = actionUrlAttribute.Value.Substring(actionUrlAttribute.Value.Length - guidLength);
                var node = actionUrlAttribute.OwnerNode;

                var actionUrlNode = node as ActionUrlNode;

                if (actionUrlNode == null)
                {
                    actionUrlNode = new ActionUrlNode(node);
                    node.ReplaceWith(actionUrlNode);
                }

                actionUrlNode.Attributes[actionUrlAttribute.Name].Value = String.Empty;
                actionUrlNode.RouteValues.Add(actionUrlAttribute.Name, _urls[key]);

                _urls.Remove(key);
            }
        }

        private static void ReplaceWith(this HtmlNode originalNode, HtmlNode newNode)
        {
            originalNode.ParentNode.ReplaceChild(newNode, originalNode);
        }

        private static void EnsureDummyViewEngineRegistered()
        {
            lock (_lockObject)
            {
                if (!ViewEngines.Engines.Contains(_viewEngine))
                {
                    ViewEngines.Engines.Clear();
                    ViewEngines.Engines.Insert(0, _viewEngine);
                }
            }
        }
        
        class DummyViewEngine : IViewEngine
        {
            public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
            {
                return new ViewEngineResult(new DummyView(partialViewName, "partial"), this);
            }

            public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
            {
                return new ViewEngineResult(new DummyView(viewName, "view"), this);
            }

            public void ReleaseView(ControllerContext controllerContext, IView view)
            {
            }
        }

        /// <summary>
        ///   Dummy placeholder view that allows parameters used to render partial views to be checked by unit tests
        ///   Everything has to go via the generated HTML, so the data is stored against a GUID, which is rendered
        ///   to the view and subsequently replaced with a strongly-typed ViewNode
        /// </summary>
        private class DummyView : IView
        {
            private readonly string _viewName;
            private readonly string _viewType;

            public DummyView(string viewName, string viewType)
            {
                _viewName = viewName;
                _viewType = viewType;
            }

            public void Render(ViewContext viewContext, TextWriter writer)
            {
                var key = Guid.NewGuid().ToString();
                var tagBuilder = new TagBuilder("placeholder");
                tagBuilder.SetInnerText(key);
                _views.Add(key, new ViewPlaceholder(_viewName, _viewType, viewContext.ViewData));
                writer.WriteLine(tagBuilder.ToString());
            }
        }

        private class ViewPlaceholder
        {
            private readonly string _viewName;
            private readonly string _viewType;
            private readonly ViewDataDictionary _viewData;

            public ViewPlaceholder(string viewName, string viewType, ViewDataDictionary viewData)
            {
                _viewName = viewName;
                _viewType = viewType;
                _viewData = viewData;
            }

            public ViewNode CreateViewNode(HtmlDocument ownerDocument)
            {
                return new ViewNode(ownerDocument, _viewName, _viewType, _viewData);
            }
        }
    }
}
