using System;
using System.IO;

using RazorGenerator.Core;
using RazorGenerator.Core.Hosting;

namespace RazorGenerator.MsBuild
{
    public static class MSBuildRazorGeneratorAssemblyLocator
    {
        private static Boolean _version1Attempted;
        private static Boolean _version2Attempted;
        private static Boolean _version3Attempted;

        private static FileInfo _version1;
        private static FileInfo _version2;
        private static FileInfo _version3;

        public static FileInfo LocateRazorGeneratorAssembly( RazorRuntime version )
        {
            switch (version)
            {
            case RazorRuntime.Version1:
                if( !_version1Attempted )
                {
                    _version1 = LocateRazorGeneratorAssemblyImpl( version );
                    _version1Attempted = true;
                }
                return _version1;

            case RazorRuntime.Version2:
                if( !_version2Attempted )
                {
                    _version2 = LocateRazorGeneratorAssemblyImpl( version );
                    _version2Attempted = true;
                }
                return _version2;

            case RazorRuntime.Version3:
                if( !_version3Attempted )
                {
                    _version3 = LocateRazorGeneratorAssemblyImpl( version );
                    _version3Attempted = true;
                }
                return _version3;

            default:
                throw new ArgumentOutOfRangeException(paramName: nameof(version), actualValue: version, message: "Unrecognized value.");
            }
        }

        private static FileInfo LocateRazorGeneratorAssemblyImpl( RazorRuntime version )
        {
            FileInfo thisAssembly = new FileInfo( typeof(MSBuildRazorGeneratorAssemblyLocator).Assembly.Location );

            DirectoryInfo directory = thisAssembly.Directory;

            IAssemblyLocator locator = new DescendantDirectoryAssemblyLocator( directory, maxDepth: 5 );

            string assemblyFileName = AssemblyLocatorExtensions.GetRazorGeneratorAssemblyFileName( version );

            return locator.LocateAssemblyOrNull( assemblyFileName );
        }
    }
}
