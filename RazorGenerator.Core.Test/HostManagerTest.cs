using System.IO;
using Xunit;

namespace RazorGenerator.Core.Test
{
    public class HostManagerTest
    {
        [Fact]
        public void HostManagerHieruisticForMvcHelperInViewsFolder()
        {
            // Arrange
            var path1 = @"Views\Helper\HtmlExtensions.cshtml";
            var path2 = @"Views\Shared\Helper\Menu.cshtml";
            var path3 = @"View\CustomModel.cshtml";

            // Act 
            var guess1 = HostManager.GuessHost(Directory.GetCurrentDirectory(), path1);
            var guess2 = HostManager.GuessHost(Directory.GetCurrentDirectory(), path2);
            var guess3 = HostManager.GuessHost(Directory.GetCurrentDirectory(), path3);

            Assert.Equal("MvcHelper", guess1);
            Assert.Equal("MvcHelper", guess2);
            Assert.Null(guess3);
        }

        [Fact]
        public void HostManagerHieruisticForMvcViews()
        {
            // Arrange
            var path1 = @"Views\Home\About.cshtml";
            var path2 = @"Views\Shared\_Layout.cshtml";

            // Act 
            var guess1 = HostManager.GuessHost(Directory.GetCurrentDirectory(), path1);
            var guess2 = HostManager.GuessHost(Directory.GetCurrentDirectory(), path2);

            Assert.Equal("MvcView", guess1);
            Assert.Equal("MvcView", guess2);
        }
    }
}
