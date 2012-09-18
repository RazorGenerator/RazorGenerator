using System.IO;
using Xunit;
using Xunit.Extensions;

namespace RazorGenerator.Core.Test
{
    public class HostManagerTest
    {
        [Theory]
        [InlineData(@"Views\Helper\HtmlExtensions.cshtml", "MvcHelper", RazorRuntime.Version1)]
        [InlineData(@"Views\Shared\Helper\Menu.cshtml", "MvcHelper", RazorRuntime.Version1)]
        [InlineData(@"View\CustomModel.cshtml", null, RazorRuntime.Version1)]
        public void HostManagerHieruisticForMvcHelperInViewsFolder(string path, string expected, RazorRuntime expectedRuntime)
        {
            // Act 
            RazorRuntime runtime;
            var guess = HostManager.GuessHost(Directory.GetCurrentDirectory(), path, out runtime);

            // Assert
            Assert.Equal(expected, guess);
            Assert.Equal(expectedRuntime, runtime);
        }

        [Theory]
        [InlineData(@"Views\Home\About.cshtml")]
        [InlineData(@"Views\Shared\_Layout.cshtml")]
        public void HostManagerHieruisticForMvcViews(string path)
        {
            // Act
            RazorRuntime runtime;
            var guess = HostManager.GuessHost(Directory.GetCurrentDirectory(), path, out runtime);

            // Assert
            Assert.Equal("MvcView", guess);
        }
    }
}
