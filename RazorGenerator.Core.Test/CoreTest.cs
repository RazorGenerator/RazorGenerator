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
        // Each Razor runtime version (v1/v2/v3) was compiled against a different
        // generation of System.Web.Razor / System.Web.Mvc / System.Web.WebPages.
        // The DLLs for v1 (Razor 1, MVC 3) and v2 (Razor 2, MVC 4) are placed in
        // "v1\" and "v2\" sub-directories next to the test assembly so they can
        // coexist with the v3 DLLs (Razor 3, MVC 5) in the main output directory.
        // The AssemblyResolve handler below steers the CLR to the correct sub-dir
        // rather than relying on app.config probing (which xunit 2.1 MSBuild runner
        // may not honour because it runs tests in-process).
        static CoreTest()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveVersionedWebAssembly;
        }

        private static Assembly ResolveVersionedWebAssembly(object sender, ResolveEventArgs args)
        {
            var requested = new AssemblyName(args.Name);
            // Only intercept the ASP.NET web stack assemblies that differ across Razor runtimes.
            var webAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System.Web.Razor",
                "System.Web.Mvc",
                "System.Web.WebPages",
                "System.Web.WebPages.Razor",
                "Microsoft.Web.Infrastructure"
            };
            if (!webAssemblies.Contains(requested.Name))
                return null;

            // Determine which versioned sub-directory to look in.
            int majorVersion = requested.Version != null ? requested.Version.Major : 0;
            string subDir;

            // System.Web.Mvc 3.x → v1, 4.x → v2
            if (requested.Name.Equals("System.Web.Mvc", StringComparison.OrdinalIgnoreCase))
                subDir = majorVersion == 3 ? "v1" : majorVersion == 4 ? "v2" : null;
            // Microsoft.Web.Infrastructure 1.x → v1
            else if (requested.Name.Equals("Microsoft.Web.Infrastructure", StringComparison.OrdinalIgnoreCase))
                subDir = "v1";
            // System.Web.Razor, System.Web.WebPages, System.Web.WebPages.Razor: 1.x → v1, 2.x → v2
            else
                subDir = majorVersion == 1 ? "v1" : majorVersion == 2 ? "v2" : null;

            if (subDir == null)
                return null;

            string outputDir = Path.GetDirectoryName(new Uri(typeof(CoreTest).Assembly.CodeBase).LocalPath);
            string dllPath = Path.Combine(outputDir, subDir, requested.Name + ".dll");
            if (File.Exists(dllPath))
                return Assembly.LoadFrom(dllPath);

            return null;
        }

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
            string workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                using (var razorGenerator = new HostManager(workingDirectory, loadExtensions: false, defaultRuntime: runtime, assemblyDirectory: Environment.CurrentDirectory))
                {
                    string inputFile = SaveInputFile(workingDirectory, testName);
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
                    Directory.Delete(workingDirectory);
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
