using System.IO;
using System.Reflection;
using Xunit;

namespace RazorGenerator.Core.Test
{
    public class HostManagerRazorVersionTest
    {
        [Theory]
        [InlineData(@"RazorVersionTest.RazorGeneratorRazorV3.csproj", RazorRuntime.Version3)]
        [InlineData(@"RazorVersionTest.ClassLibrary.csproj", null)]
        [InlineData(@"RazorVersionTest.ClassLibraryNETStandard.csproj", null)]
        [InlineData(@"RazorVersionTest.ConsoleApp.csproj", null)]
        [InlineData(@"RazorVersionTest.ConsoleAppWithPackageReference.csproj", null)]
        [InlineData(@"RazorVersionTest.CoreWithPackageReference.csproj", null)]
        [InlineData(@"RazorVersionTest.MvcRazorV3.csproj", RazorRuntime.Version3)]
        [InlineData(@"RazorVersionTest.MvcCoreWithPackageReferenceRazorV2.csproj", RazorRuntime.Version2)]
        [InlineData(@"RazorVersionTest.MvcWithPackageReferenceRazorV3.csproj", RazorRuntime.Version3)]
        [InlineData(@"RazorVersionTest.SampleMvcRazorV3.csproj", RazorRuntime.Version3)]
        public void HostManager_GetRazorRuntimeVersion_ReturnExpectedVersion(string path, RazorRuntime? expectedRuntime)
        {
            var fileContent = GetManifestFileContent(path);
            var runtime = HostManager.GetRazorRuntimeVersion(fileContent);
            Assert.Equal(expectedRuntime, runtime);
        }

        private static string GetManifestFileContent(string filepath)
        {
            var resourceName = string.Join(".", "RazorGenerator.Core.Test.TestFiles", filepath);

            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
