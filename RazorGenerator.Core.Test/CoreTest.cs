using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace RazorGenerator.Core.Test
{
    public class CoreTest
    {
        private static readonly string[] _testNames = new[] 
        { 
            "WebPageTest",
            "WebPageHelperTest",
             "MvcViewTest",
            "MvcHelperTest",
            "TemplateTest",
            "_ViewStart",
            "DirectivesTest",
            "TemplateWithBaseTypeTest",
            "TemplateWithGenericParametersTest",
            "VirtualPathAttributeTest",
            "SuffixTransformerTest"
        };

        [Theory]
        [MemberData("V1Tests")]
        [MemberData("V2Tests")]
        [MemberData("V3Tests")]
        public void TestTransformerType(string testName, RazorRuntime runtime)
        {
            DirectoryInfo workingDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            DirectoryInfo currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            try
            {
                using (var razorGenerator = new HostManager(workingDirectory, loadExtensions: false, defaultRuntime: runtime, assemblyDirectory: currentDirectory))
                {
                    FileInfo inputFile = SaveInputFile(workingDirectory.FullName, testName);
                    var host = razorGenerator.CreateHost(inputFile, testName + ".cshtml", string.Empty);
                    host.DefaultNamespace = GetType().Namespace;
                    host.EnableLinePragmas = false;

                    var output = host.GenerateCode();
                    AssertOutput(testName, output, runtime);
                }
            }
            finally
            {
                try
                {
                    workingDirectory.Delete();
                }
                catch
                {
                }
            }

        }

        public static IEnumerable<object[]> V1Tests
        {
            get
            {
                return _testNames.Select(c => new object[] { c, RazorRuntime.Version1 });
            }
        }

        public static IEnumerable<object[]> V2Tests
        {
            get
            {
                return _testNames.Select(c => new object[] { c, RazorRuntime.Version2 });
            }
        }

        public static IEnumerable<object[]> V3Tests
        {
            get
            {
                return _testNames.Select(c => new object[] { c, RazorRuntime.Version3 });
            }
        }

        private static FileInfo SaveInputFile(string outputDirectory, string testName)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            string outputFile = Path.Combine(outputDirectory, testName + ".cshtml");
            File.WriteAllText(outputFile, GetManifestFileContent(testName, "Input"));
            return new FileInfo(outputFile);
        }

        private static void AssertOutput(string testName, string output, RazorRuntime runtime)
        {
            var expectedContent = GetManifestFileContent(testName, "Output_v" + (int)runtime);
            output = Regex.Replace(output, @"Runtime Version:[\d.]*", "Runtime Version:N.N.NNNNN.N")
                          .Replace(typeof(HostManager).Assembly.GetName().Version.ToString(), "v.v.v.v");

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
