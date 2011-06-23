using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RazorGenerator.Core.Test {
    [TestClass]
    public class HostManagerTest {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void HostManagerHieruisticForMvcHelperInViewsFolder() {
            // Arrange
            var path1 = @"Views\Helper\HtmlExtensions.cshtml";
            var path2 = @"Views\Shared\Helper\Menu.cshtml";
            var path3 = @"View\CustomModel.cshtml";

            // Act 
            var guess1 = HostManager.GuessHost(TestContext.TestDeploymentDir, path1);
            var guess2 = HostManager.GuessHost(TestContext.TestDeploymentDir, path2);
            var guess3 = HostManager.GuessHost(TestContext.TestDeploymentDir, path3);

            Assert.AreEqual("MvcHelper", guess1);
            Assert.AreEqual("MvcHelper", guess2);
            Assert.IsNull(guess3);
        }

        [TestMethod]
        public void HostManagerHieruisticForMvcViews() {
            // Arrange
            var path1 = @"Views\Home\About.cshtml";
            var path2 = @"Views\Shared\_Layout.cshtml";

            // Act 
            var guess1 = HostManager.GuessHost(TestContext.TestDeploymentDir, path1);
            var guess2 = HostManager.GuessHost(TestContext.TestDeploymentDir, path2);

            Assert.AreEqual("MvcView", guess1);
            Assert.AreEqual("MvcView", guess2);
        }
    }
}
