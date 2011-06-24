using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RazorGenerator.Core.Test {
    [TestClass]
    public class CoreTest {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void WebPageTest() {
            TestTransformerType();
        }

        [TestMethod]
        public void WebPageHelperTest() {
            TestTransformerType();
        }

        [TestMethod]
        public void MvcHelperTest() {
            TestTransformerType();
        }

        [TestMethod]
        public void MvcViewTest() {
            TestTransformerType();
        }

        [TestMethod]
        public void TemplateTest() {
            TestTransformerType();
        }

        [TestMethod]
        public void _ViewStart() {
            TestTransformerType();
        }

        [TestMethod]
        public void DirectivesTest() {
            TestTransformerType();
        }

        private void TestTransformerType() {
            using (var razorGenerator = new HostManager(TestContext.TestDeploymentDir)) {
                string inputFile = SaveInputFile(TestContext);
                var host = razorGenerator.CreateHost(inputFile, TestContext.TestName + ".cshtml");
                host.DefaultNamespace = GetType().Namespace;
                host.EnableLinePragmas = false;

                var output = host.GenerateCode();
                AssertOutput(TestContext, output);
            }
        }

        private static string SaveInputFile(TestContext testContext) {
            string outputFile = Path.Combine(testContext.TestDeploymentDir, testContext.TestName);
            File.WriteAllText(outputFile, GetManifestFileContent(testContext, "Input"));
            return outputFile;
        }

        private static void AssertOutput(TestContext testContext, string output) {
            var expectedContent = GetManifestFileContent(testContext, "Output");
            output = Regex.Replace(output, @"Runtime Version:[\d.]*", "Runtime Version:N.N.NNNNN.N");
            expectedContent = expectedContent.Replace("v.v.v.v", typeof(HostManager).Assembly.GetName().Version.ToString());

            Assert.AreEqual(expectedContent, output);
        }

        private static string GetManifestFileContent(TestContext testContext, string fileType) {
            var extension = fileType.Equals("Input", StringComparison.OrdinalIgnoreCase) ? "cshtml" : "txt";
            var resourceName = String.Join(".", "RazorGenerator.Core.Test.TestFiles", fileType, testContext.TestName, extension);

            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))) {
                return reader.ReadToEnd();
            }
        }
    }
}
