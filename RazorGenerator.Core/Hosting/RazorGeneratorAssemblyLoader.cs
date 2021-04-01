using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RazorGenerator.Core.Hosting
{
    public interface IAssemblyLocator
    {
        FileInfo LocateAssemblyOrNull( String name );
    }

    public static class AssemblyLocatorExtensions
    {
        public static FileInfo GetRazorGeneratorAssemblyFile( this IAssemblyLocator locator, RazorRuntime runtime)
        {
            String fileName = GetRazorGeneratorAssemblyFileName( version: runtime );

            return locator.LocateAssemblyOrNull( fileName );
        }

        public static Assembly GetAssembly( this IAssemblyLocator locator, RazorRuntime runtime)
        {
            throw new NotImplementedException();
            /*

            int runtimeValue = (int)runtime;
            // TODO: Check if we can switch to using CodeBase instead of Location

            // Look for the assembly at vX\RazorGenerator.vX.dll. If not, assume it is at RazorGenerator.vX.dll
            string runtimeDirectory = Path.Combine(this.assemblyDirectory.FullName, "v" + runtimeValue);
            string assemblyName = "RazorGenerator.Core.v" + runtimeValue + ".dll";
            string runtimeDirPath = Path.Combine(runtimeDirectory, assemblyName);
            if (File.Exists(runtimeDirPath))
            {
                Assembly assembly = Assembly.LoadFrom(runtimeDirPath);
                return assembly;
            }
            else
            {
                return Assembly.LoadFrom(Path.Combine(this.assemblyDirectory.FullName, assemblyName));
            }
            */
        }

        public static Assembly OnAssemblyResolve(object sender, ResolveEventArgs eventArgs)
        {
            throw new NotImplementedException();
            /*
            AssemblyName nameToResolve = new AssemblyName(eventArgs.Name);
            string path = Path.Combine(this.assemblyDirectory.FullName, "v" + nameToResolve.Version.Major, nameToResolve.Name) + ".dll"; // hold on, there's the "RazorGenerator.v{n].dll"?
            if (File.Exists(path))
            {
                return Assembly.LoadFrom(path);
            }
            return null;
            */
        }

        public static String GetRazorGeneratorAssemblyFileName( RazorRuntime version )
        {
            switch (version)
            {
            case RazorRuntime.Version1:
                return "RazorGenerator.Core.v1.dll";

            case RazorRuntime.Version2:
                return "RazorGenerator.Core.v2.dll";

            case RazorRuntime.Version3:
                return "RazorGenerator.Core.v3.dll";

            default:
                throw new ArgumentOutOfRangeException(paramName: nameof(version), actualValue: version, message: "Unrecognized value.");
            }
        }
    }

    public class SingleDirectoryAssemblyLocator : IAssemblyLocator
    {
        public SingleDirectoryAssemblyLocator(DirectoryInfo directory)
        {
            this.Directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public DirectoryInfo Directory { get; }

        public FileInfo LocateAssemblyOrNull(string name)
        {
            // Question: what form does `name` take? does it include the `.dll` file extension? what about versioning?

            String expectedPath = Path.Combine( this.Directory.FullName, name );
            FileInfo test1 = new FileInfo( expectedPath );
            if( test1.Exists ) return test1;
            
            return null;
        }
    }

    public class PackagesDirectoryAssemblyLocator : IAssemblyLocator
    {
        public PackagesDirectoryAssemblyLocator(DirectoryInfo packagesDirectory)
        {
            this.PackagesDirectory = packagesDirectory ?? throw new ArgumentNullException(nameof(packagesDirectory));
        }

        public DirectoryInfo PackagesDirectory { get; }

        public FileInfo LocateAssemblyOrNull(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class DescendantDirectoryAssemblyLocator : IAssemblyLocator
    {
        private readonly Int32 maxDepth;

        public DescendantDirectoryAssemblyLocator(DirectoryInfo directory, Int32 maxDepth)
        {
            this.Directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.maxDepth  = maxDepth;
        }

        public DirectoryInfo Directory { get; }

        public FileInfo LocateAssemblyOrNull(string name)
        {
            return this.Dfs( dir: this.Directory, depth: 1, fileName: name );
        }

        private FileInfo Dfs( DirectoryInfo dir, Int32 depth, String fileName )
        {
            if( depth >= this.maxDepth )
            {
                return null;
            }

            FileInfo[] dllFiles = dir.GetFiles( fileName );
            if( dllFiles.Length == 1 )
            {
                return dllFiles[0];
            }

            DirectoryInfo[] children = dir.GetDirectories();
            foreach( DirectoryInfo child in children )
            {
                FileInfo match = this.Dfs( child, depth: depth + 1, fileName: fileName );
                if( match != null )
                {
                    return match;
                }
            }

            return null;
        }
    }
}
