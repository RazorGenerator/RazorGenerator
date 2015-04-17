using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrecompiledMvcLibrary.Views.Home;
using RazorGenerator.Testing;

namespace MvcViewsTests
{
    using System.Web;
    using System.Web.Routing;
    using Moq;
    using MvcSample;

    [TestClass]
    public class TestViews
    {
        [TestMethod]
        public void TestRenderPlainText()
        {
            const string message = "Some unit test message!";

            // Instantiate the view directly
            var view = new ExternalPrecompiled();

            // Set up the data that needs to be access by the view
            view.ViewBag.Message = message;

            // Render it as a string
            var output = view.Render();

            // Verify that it looks correct
            Assert.IsTrue(output.Contains(message));
        }

        [TestMethod]
        public void TestRenderPlainTextWithModel()
        {
            const string message = "Some model data!";

            // Instantiate the view directly
            var view = new ExternalPrecompiled();

            // Render it as a string
            var output = view.Render(message);

            // Verify that it looks correct
            Assert.IsTrue(output.Contains(message));
        }

        [TestMethod]
        public void TestRenderAsHtml()
        {
            const string message = "Some unit test message!";

            // Instantiate the view directly
            var view = new ExternalPrecompiled();

            // Set up the data that needs to be access by the view
            view.ViewBag.Message = message;

            // Render it in an HtmlDocument
            var doc = view.RenderAsHtml();

            // Verify that it looks correct
            HtmlNode node = doc.DocumentNode.Element("h2");
            Assert.AreEqual(message, node.InnerHtml.Trim());
        }

        // This became broken with newer versions of MVC. Needs investigation
        [Ignore]
        [TestMethod]
        public void TestLayout()
        {
            const string title = "Some unit test title!";

            // Instantiate the view directly
            var view = new PrecompiledMvcLibrary.Views.Shared._Layout();

            // Set up the data that needs to be access by the view
            view.ViewBag.Title = title;

            // Render it as a string
            var output = view.Render();

            // Verify that it looks correct
            Assert.IsTrue(output.Contains(title));
        }

        [TestMethod]
        public void TestUsingRoutes()
        {
            // Instantiate the view directly
            var view = new UsingRoutes();

            // Set up the data that needs to be access by the view
            MvcApplication.RegisterRoutes(RouteTable.Routes);

            // Render it in an HtmlDocument
            var output = view.RenderAsHtml();

            // Verify that it looks correct
            var element = output.GetElementbyId("link-using-routes");
            Assert.IsNotNull(element);
            Assert.AreEqual("/Home/UsingRoutes", element.Attributes["href"].Value);
        }

        [TestMethod]
        public void TestMockHttpContext()
        {
            // Instantiate the view directly
            var view = new MockHttpContext();

            // Set up the data that needs to be access by the view
            var mockHttpRequest = new Mock<HttpRequestBase>(MockBehavior.Loose);
            mockHttpRequest.Setup(m => m.IsAuthenticated).Returns(true);

            // Render it in an HtmlDocument
            var output = view.RenderAsHtml(new HttpContextBuilder().With(mockHttpRequest.Object).Build());

            // Verify that it looks correct
            var element = output.GetElementbyId("user-authenticated");
            Assert.IsNotNull(element);   
        }
    }
}
