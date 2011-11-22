using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Extensions;

namespace RazorGenerator.Core.Test
{
    public class CoreTest
    {
        [Theory]
        [InlineData(new object[] { "WebPageTest" })]
        [InlineData(new object[] { "WebPageHelperTest" })]
        [InlineData(new object[] { "MvcViewTest" })]
        [InlineData(new object[] { "MvcHelperTest" })]
        [InlineData(new object[] { "TemplateTest" })]
        [InlineData(new object[] { "_ViewStart" })]
        [InlineData(new object[] { "DirectivesTest" })]
        [InlineData(new object[] { "TemplateWithBaseTypeTest" })]
        [InlineData(new object[] { "VirtualPathAttributeTest" })]
        public void TestTransformerType(string testName)
        {
            string workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            using (var razorGenerator = new HostManager(workingDirectory, loadExtensions: false))
            {
                string inputFile = SaveInputFile(workingDirectory, testName);
                var host = razorGenerator.CreateHost(inputFile, testName + ".cshtml");
                host.DefaultNamespace = GetType().Namespace;
                host.EnableLinePragmas = false;

                var output = host.GenerateCode();
                AssertOutput(testName, output);
            }
        }

        private static string SaveInputFile(string outputDirectory, string testName)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            string outputFile = Path.Combine(outputDirectory, testName + ".cshtml");
            File.WriteAllText(outputFile, GetManifestFileContent(testName, "Input"));
            return outputFile;
        }

        private static void AssertOutput(string testName, string output)
        {
            var expectedContent = GetManifestFileContent(testName, "Output");
            output = Regex.Replace(output, @"Runtime Version:[\d.]*", "Runtime Version:N.N.NNNNN.N");
            expectedContent = expectedContent.Replace("v.v.v.v", typeof(HostManager).Assembly.GetName().Version.ToString());

            Assert.Equal(expectedContent, output);
        }

        private static string GetManifestFileContent(string testName, string fileType)
        {
            var extension = fileType.Equals("Input", StringComparison.OrdinalIgnoreCase) ? "cshtml" : "txt";
            var resourceName = String.Join(".", "RazorGenerator.Core.Test.TestFiles", fileType, testName, extension);

            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
