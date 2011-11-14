using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrecompiledMvcLibrary.Views.Home;
using RazorGenerator.Testing;

namespace MvcViewsTests
{
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
    }
}
