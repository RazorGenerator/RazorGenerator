using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using RazorGenerator.Core.Hosting;

namespace RazorGenerator.Core.Test
{
    // https://stackoverflow.com/questions/43927955/should-getenvironmentvariable-work-in-xunit-test
    public sealed class RazorAssemblyLocationFixture : IDisposable
    {
        #region NuGet /packages/ directory

        private static DirectoryInfo GetNuGetPackagesDirectory()
        {
            Assembly testAssembly = Assembly.GetExecutingAssembly();
            DirectoryInfo testBinariesLocation = new DirectoryInfo( Path.GetDirectoryName( testAssembly.Location ) );

            DirectoryInfo candidate = testBinariesLocation;
            while( candidate != null && candidate.Parent != null && ( candidate.Parent != candidate ) )
            {
                if( TryGetClassicPackagesDirectory( candidate, out DirectoryInfo packagesDir ) )
                {
                    return packagesDir;
                }

                candidate = candidate.Parent;
            }

            throw new InvalidOperationException( "Failed to find the NuGet 'packages' directory by searching ancestors of \"" + testBinariesLocation + "\"." );
        }

        /// <summary>Attempts to see if <paramref name="solutionRootCandidate"/> contains a directory named &quot;packages&quot; (which contains known packages). If so, then it sets <paramref name="packages"/> accordingly and returns <see langword="true" />, otherwise returns <see langword="false"/>.</summary>
        private static Boolean TryGetClassicPackagesDirectory( DirectoryInfo solutionRootCandidate, out DirectoryInfo packages )
        {
            DirectoryInfo[] packagesChildDir = solutionRootCandidate.GetDirectories( searchPattern: "packages", SearchOption.TopDirectoryOnly );
            if( packagesChildDir?.Length == 1 )
            {
                packages = packagesChildDir[0];

                return IsClassicPackagesDirectory( packages );
            }
            else
            {
                packages = default;
                return false;
            }
        }

        private static Boolean IsClassicPackagesDirectory( DirectoryInfo candidate )
        {
            if( "packages".Equals( candidate.Name, StringComparison.OrdinalIgnoreCase ) )
            {
                DirectoryInfo[] children = candidate.GetDirectories();
                if( children.Any( d => d.Name.StartsWith( "Microsoft.AspNet.", StringComparison.OrdinalIgnoreCase ) ) )
                {
                    return true;
                }
            }

            return false;
        }

        private static Boolean IsSolutionRootDirectory( DirectoryInfo candidate )
        {
            HashSet<String> fileNames = candidate.GetFiles()      .Select( fi => fi.Name ).ToHashSet( StringComparer.OrdinalIgnoreCase );
            HashSet<String> subdirs   = candidate.GetDirectories().Select( di => di.Name ).ToHashSet( StringComparer.OrdinalIgnoreCase );

            // Required files:

            return
                fileNames.Contains( "RazorGenerator.Runtime.sln" ) &&
                fileNames.Contains( "RazorGenerator.Tooling.sln" ) &&
                subdirs.Contains( "packages" ) &&
                subdirs.Contains( "RazorGenerator.Core.v1" ) &&
                subdirs.Contains( "RazorGenerator.Core.v2" ) &&
                subdirs.Contains( "RazorGenerator.Core.v3" )
            ;
        }

        #endregion

        //

        public RazorAssemblyLocationFixture()
        {
            this.PackagesDirectory = GetNuGetPackagesDirectory();

            this.SolutionRootDirectory = this.PackagesDirectory.Parent;

            if( !IsSolutionRootDirectory( this.SolutionRootDirectory ) )
            {
                throw new InvalidOperationException( "Could not find solution root directory." );
            }
        }

        public void Dispose()
        {
        }

        public DirectoryInfo PackagesDirectory { get; }

        public DirectoryInfo SolutionRootDirectory { get; }

        public FileInfo GetRazorGeneratorAssemblyFileInfo( RazorRuntime version, AssemblyConfiguration configuration )
        {
            String projectDirectoryName = GetProjectDirectoryName( version );
            String outputDirectoryName  = configuration == AssemblyConfiguration.Debug ? "Debug" : "Release";
            String assemblyFileName     = AssemblyLocatorExtensions.GetRazorGeneratorAssemblyFileName( version );
            String assemblyFilePath     = Path.Combine( this.SolutionRootDirectory.FullName, projectDirectoryName, "bin", outputDirectoryName, assemblyFileName );

            return new FileInfo( assemblyFilePath );
        }

        private static String GetProjectDirectoryName( RazorRuntime version )
        {
            switch (version)
            {
            case RazorRuntime.Version1:
                return "RazorGenerator.Core.v1";

            case RazorRuntime.Version2:
                return "RazorGenerator.Core.v2";

            case RazorRuntime.Version3:
                return "RazorGenerator.Core.v3";

            default:
                throw new ArgumentOutOfRangeException(paramName: nameof(version), actualValue: version, message: "Unrecognized value.");
            }
        }

#if NOT_NOW

        //

        // TODO: Remove this, methinks. The idea being that RazorGenerator.Core.Test should not need any direct references to the v1/v2/v3 assemblies as they're all loaded dynamically anyway, no?
        public IReadOnlyList<FileInfo> GetRazorAssemblyFiles( RazorRuntime razorRuntime )
        {
            switch (razorRuntime)
            {
            case RazorRuntime.Version1:
                return this.GetRazorAssemblyFiles( typeof(RazorGenerator.Core.Version1RazorHostProvider).Assembly );

            case RazorRuntime.Version2:
                return this.GetRazorAssemblyFiles( typeof(RazorGenerator.Core.Version2RazorHostProvider).Assembly );

            case RazorRuntime.Version3:
                return this.GetRazorAssemblyFiles( typeof(RazorGenerator.Core.Version3RazorHostProvider).Assembly );

            default:
                throw new ArgumentOutOfRangeException(nameof(razorRuntime), actualValue: razorRuntime, message: "Unrecognized value.");
            }
        }

        

//        public IReadOnlyList<FileInfo> GetRazorAssemblyFiles( Assembly razorGeneratorVersionedAssembly )
//        {
//            AssemblyName[] referencedAssemblies = razorGeneratorVersionedAssembly.GetReferencedAssemblies();
//
//            return Array.Empty<FileInfo>();
//        }
//
//        public RazorRuntimeAssemblyResolver CreateResolver( RazorRuntime razorRuntime )
//        {
//
//        }
#endif
    }

    public enum AssemblyConfiguration
    {
        Debug,
        Release
    }


    //    public class RazorRuntimeAssemblyResolver
    //    {
    //        public RazorRuntimeAssemblyResolver()
    //        {
    //
    //        }
    //    }
}
