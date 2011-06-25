using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrecompiledMvcLibrary.Views.Home;
using PrecompiledMvcViews.Testing;

namespace MvcViewsTests {
    [TestClass]
    public class TestViews {
        [TestMethod]
        public void TestMethodWithAgilityPack() {
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
    }
}
