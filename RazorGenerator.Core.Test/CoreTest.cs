using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace RazorGenerator.Core.Test
{
    public class CoreTest : IClassFixture<RazorAssemblyLocationFixture>
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

        private readonly RazorAssemblyLocationFixture assemblies;

        public CoreTest(RazorAssemblyLocationFixture assemblies)
        {
            this.assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
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

        static class AppDomainDataKeys
        {
            public const String IS_TEST_APP_DOMAIN     = nameof(IS_TEST_APP_DOMAIN);
            public const String MAIN_APPDOMAIN_ID      = nameof(MAIN_APPDOMAIN_ID);
            public const String ASSEMBLY_FILE_NAME_KEY = nameof(ASSEMBLY_FILE_NAME_KEY);
            public const String TEST_NAME              = nameof(TEST_NAME);
            public const String RUNTIME_VERSION        = nameof(RUNTIME_VERSION);

            public const String TEST_RESULT            = nameof(TEST_RESULT);
            public const String EXCEPTION              = nameof(EXCEPTION);
        }

        // FYI: The `MemberData` attribute instructs xUnit to get `TestTransformerType`'s runtime parameter arguments from `CoreTest.V1Tests`, `CoreTest.V2Tests`, etc.
        // Though this test *could* use [InlineData] as the set of testNames is the same for all 3 versions... for now, but in future this may change.
        [Theory]
        [MemberData( memberName: nameof(V1Tests) )]
        [MemberData( memberName: nameof(V2Tests) )]
        [MemberData( memberName: nameof(V3Tests) )]
        public /*async Task*/ void TestTransformerType(string testName, RazorRuntime runtime)
        {
#if SELF_MANAGED_APPDOMAINS
            AppDomain testAppDomain = AppDomain.CreateDomain( friendlyName: "AppDomain for test {0} under {1}".Fmt( testName, runtime ) );
            try
            {
                _ = testAppDomain.Load( Assembly.GetExecutingAssembly().GetName() );

                FileInfo razorGeneratorAssemblyFileName = this.assemblies.GetRazorGeneratorAssemblyFileInfo( runtime, AssemblyConfiguration.Debug );

                testAppDomain.SetData( AppDomainDataKeys.IS_TEST_APP_DOMAIN    , true );
                testAppDomain.SetData( AppDomainDataKeys.MAIN_APPDOMAIN_ID     , AppDomain.CurrentDomain.Id );
                testAppDomain.SetData( AppDomainDataKeys.ASSEMBLY_FILE_NAME_KEY, razorGeneratorAssemblyFileName.FullName );
                testAppDomain.SetData( AppDomainDataKeys.TEST_NAME             , testName );
                testAppDomain.SetData( AppDomainDataKeys.RUNTIME_VERSION       , (Int32)runtime );
                
                testAppDomain.DoCallBack( RunRazorHostManagerInAppDomain );

                Object testResult = testAppDomain.GetData( AppDomainDataKeys.TEST_RESULT );
                if( testResult is Boolean ok )
                {
                    // OK
                    Assert.True( ok );
                }
                else if( testResult is String errorReport )
                {
                    Assert.True( false, userMessage: errorReport );
                }
                else
                {
                    Assert.True( false, userMessage: "AppDomain TEST_RESULT is invalid." );
                }
            }
            finally
            {
                AppDomain.Unload( testAppDomain );
            }
#else
            FileInfo razorGeneratorAssemblyFile = this.assemblies.GetRazorGeneratorAssemblyFileInfo( version: runtime, configuration: AssemblyConfiguration.Debug );

            RunRazorHostManagerInAppDomain( runtime, testName, razorGeneratorAssemblyFilePath: razorGeneratorAssemblyFile );
#endif
        }

        private static void RunRazorHostManagerInAppDomain()
        {
            AppDomain testAppDomain = AppDomain.CurrentDomain;

            // Sanity-check:
            if( testAppDomain.GetData( AppDomainDataKeys.IS_TEST_APP_DOMAIN ) is Boolean isTestAppDomain && isTestAppDomain == true )
            {
                // OK
            }
            else
            {
                testAppDomain.SetData( AppDomainDataKeys.TEST_RESULT, nameof(RunRazorHostManagerInAppDomain) + " is not being executed in a test AppDomain." );
                return;
            }
            
            //

            RazorRuntime runtimeVersion                 = (RazorRuntime)(Int32)testAppDomain.GetData( AppDomainDataKeys.RUNTIME_VERSION );
            String       testName                       = (String)             testAppDomain.GetData( AppDomainDataKeys.TEST_NAME );
            String       razorGeneratorAssemblyFilePath = (String)             testAppDomain.GetData( AppDomainDataKeys.ASSEMBLY_FILE_NAME_KEY );

            FileInfo     razorGeneratorAssemblyFile = new FileInfo( razorGeneratorAssemblyFilePath );

            try
            {
                RunRazorHostManagerInAppDomain( runtimeVersion, testName, razorGeneratorAssemblyFile );
            }
            catch( Exception ex )
            {
                testAppDomain.SetData( AppDomainDataKeys.EXCEPTION, ex ); // <-- This should be okay as Exceptions are ostensibly serializable.
            }
        }

        private static void RunRazorHostManagerInAppDomain( RazorRuntime runtimeVersion, String testName, FileInfo razorGeneratorAssemblyFilePath )
        {
            DirectoryInfo workingDirectory = new DirectoryInfo(Path.GetTempPath());
            try
            {
                using (RazorHostManager razorGenerator = new RazorHostManager( baseDirectory: workingDirectory, loadExtensions: false, razorGeneratorAssemblyFile: razorGeneratorAssemblyFilePath ))//  defaultRuntime: runtime, assemblyDirectory: assemblyDirectory))
                {
                    FileInfo inputFile = SaveInputFile(workingDirectory.FullName, testName);
                    IRazorHost host = razorGenerator.CreateHost(inputFile, testName + ".cshtml", string.Empty);
                    host.DefaultNamespace  = "RazorGenerator.Core.Test"; // TODO: Change this to something less self-referential.
                    host.EnableLinePragmas = false;

                    string output = host.GenerateCode();
                    AssertOutput(testName, output, runtimeVersion);
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

        private static FileInfo SaveInputFile(string outputDirectory, string testName)
        {
            if (!Directory.Exists(outputDirectory))
            {
                _ = Directory.CreateDirectory(outputDirectory);
            }

            string outputFile = Path.Combine(outputDirectory, testName + ".cshtml");
            File.WriteAllText(outputFile, GetManifestFileContent(testName, fileType: "Input"));
            return new FileInfo(outputFile);
        }

        private static void AssertOutput(string testName, string output, RazorRuntime runtime)
        {
            string expectedContent = GetManifestFileContent(testName, "Output_v" + (int)runtime);

            string assemblyVersionStr = typeof(RazorHostManager).Assembly.GetName().Version.ToString();

            output = Regex
                .Replace(output, @"Runtime Version:[\d.]*", "Runtime Version:N.N.NNNNN.N")
                .Replace(assemblyVersionStr, "v.v.v.v");

            Assert.Equal(expectedContent, output);
        }

        private static string GetManifestFileContent(string testName, string fileType)
        {
            string extension = fileType.Equals("Input", StringComparison.OrdinalIgnoreCase) ? "cshtml" : "txt";
            string resourceName = String.Join(separator: ".", "RazorGenerator.Core.Test.TestFiles", fileType, testName, extension);

            using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
