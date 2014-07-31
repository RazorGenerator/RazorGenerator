using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace RazorGenerator.Core
{
    public class HostManager : IDisposable
    {
        private readonly string _baseDirectory;
        private readonly bool _loadExtensions;
        private readonly RazorRuntime _defaultRuntime;
        private readonly string _assemblyDirectory;
        private ComposablePartCatalog _catalog;

        public HostManager(string baseDirectory)
            : this(baseDirectory, loadExtensions: true, defaultRuntime: RazorRuntime.Version1, assemblyDirectory: GetAssesmblyDirectory())
        {
        }

        public HostManager(string baseDirectory, bool loadExtensions, RazorRuntime defaultRuntime, string assemblyDirectory)
        {
            _loadExtensions = loadExtensions;
            _baseDirectory = baseDirectory;
            _defaultRuntime = defaultRuntime;
            _assemblyDirectory = assemblyDirectory;

            // Repurposing loadExtensions to mean unit-test scenarios. Don't bind to the AssemblyResolve in unit tests
            if (_loadExtensions)
            {
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            }
        }

        public IRazorHost CreateHost(string fullPath, string projectRelativePath)
        {
            using (var codeDomProvider = new CSharpCodeProvider())
            {
                return CreateHost(fullPath, projectRelativePath, codeDomProvider);
            }
        }

        public IRazorHost CreateHost(string fullPath, string projectRelativePath, CodeDomProvider codeDomProvider)
        {
            var directives = DirectivesParser.ParseDirectives(_baseDirectory, fullPath);

            string hostName;
            RazorRuntime runtime = _defaultRuntime;
            if (!directives.TryGetValue("Generator", out hostName))
            {
                // Determine the host and runtime from the file \ project
                hostName = GuessHost(_baseDirectory, projectRelativePath, out runtime);
            }
            string razorVersion;
            if (directives.TryGetValue("RazorVersion", out razorVersion))
            {
                // If the directive explicitly specifies a host, use that.
                switch (razorVersion)
                {
                    case "1":
                        runtime = RazorRuntime.Version1;
                        break;
                    case "2":
                        runtime = RazorRuntime.Version2;
                        break;
                    default:
                        runtime = RazorRuntime.Version3;
                        break;
                }
            }

            if (_catalog == null)
            {
                _catalog = InitCompositionCatalog(_baseDirectory, _loadExtensions, runtime);
            }

            using (var container = new CompositionContainer(_catalog))
            {
                var codeTransformer = GetRazorCodeTransformer(container, projectRelativePath, hostName);
                var host = container.GetExport<IHostProvider>().Value;
                return host.GetRazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives);
            }
        }

        private IRazorCodeTransformer GetRazorCodeTransformer(CompositionContainer container, string projectRelativePath, string hostName)
        {

            IRazorCodeTransformer codeTransformer = null;
            try
            {
                codeTransformer = container.GetExportedValue<IRazorCodeTransformer>(hostName);
            }
            catch (Exception exception)
            {
                string availableHosts = String.Join(", ", GetAvailableHosts(container));
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, RazorGeneratorResources.GeneratorFailureMessage, projectRelativePath, availableHosts), exception);
            }

            if (codeTransformer == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, RazorGeneratorResources.GeneratorError_UnknownGenerator, hostName));
            }
            return codeTransformer;
        }

        private ComposablePartCatalog InitCompositionCatalog(string baseDirectory, bool loadExtensions, RazorRuntime runtime)
        {
            // Retrieve available hosts
            var hostsAssembly = GetAssembly(runtime);
            var catalog = new AggregateCatalog(new AssemblyCatalog(hostsAssembly));

            if (loadExtensions)
            {
                // We assume that the baseDirectory points to the project root. Look for the RazorHosts directory under the project root
                AddCatalogIfHostsDirectoryExists(catalog, baseDirectory);

                // Look for the Razor Hosts directory up to two directories above the baseDirectory. Hopefully this should cover the solution root.
                var solutionDirectory = Path.Combine(baseDirectory, @"..\");
                AddCatalogIfHostsDirectoryExists(catalog, solutionDirectory);

                solutionDirectory = Path.Combine(baseDirectory, @"..\..\");
                AddCatalogIfHostsDirectoryExists(catalog, solutionDirectory);
            }

            return catalog;
        }

        private static IEnumerable<string> GetAvailableHosts(CompositionContainer container)
        {
            // We need for a way to figure out what the exporting type is. This could return arbitrary exports that are not ISingleFileGenerators
            return from part in container.Catalog.Parts
                   from export in part.ExportDefinitions
                   where !String.IsNullOrEmpty(export.ContractName)
                   select export.ContractName;
        }

        private Assembly GetAssembly(RazorRuntime runtime)
        {
            int runtimeValue = (int)runtime;
            // TODO: Check if we can switch to using CodeBase instead of Location

            // Look for the assembly at vX\RazorGenerator.vX.dll. If not, assume it is at RazorGenerator.vX.dll
            string runtimeDirectory = Path.Combine(_assemblyDirectory, "v" + runtimeValue);
            string assemblyName = "RazorGenerator.Core.v" + runtimeValue + ".dll";
            string runtimeDirPath = Path.Combine(runtimeDirectory, assemblyName);
            if (File.Exists(runtimeDirPath))
            {
                Assembly assembly = Assembly.LoadFrom(runtimeDirPath);

                return assembly;
            }
            else
            {
                return Assembly.LoadFrom(Path.Combine(_assemblyDirectory, assemblyName));
            }
        }

        internal static string GuessHost(string projectRoot, string projectRelativePath, out RazorRuntime runtime)
        {
            bool isMvcProject = IsMvcProject(projectRoot, out runtime) ?? false;
            if (isMvcProject)
            {
                var mvcHelperRegex = new Regex(@"(^|\\)Views(\\.*)+Helpers?", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
                if (mvcHelperRegex.IsMatch(projectRelativePath))
                {
                    return "MvcHelper";
                }
                return "MvcView";
            }
            return null;
        }

        private static bool? IsMvcProject(string projectRoot, out RazorRuntime razorRuntime)
        {
            razorRuntime = RazorRuntime.Version1;
            try
            {
                var projectFile = Directory.EnumerateFiles(projectRoot, "*.csproj").FirstOrDefault();
                if (projectFile != null)
                {
                    var content = File.ReadAllText(projectFile);
                    if ((content.IndexOf("System.Web.Mvc, Version=4", StringComparison.OrdinalIgnoreCase) != -1) ||
                        (content.IndexOf("System.Web.Razor, Version=2", StringComparison.OrdinalIgnoreCase) != -1) ||
                        (content.IndexOf("Microsoft.AspNet.Mvc.4", StringComparison.OrdinalIgnoreCase) != -1))
                    {
                        // The project references Razor v2
                        razorRuntime = RazorRuntime.Version2;
                    }
                    else if ((content.IndexOf("System.Web.Mvc, Version=5", StringComparison.OrdinalIgnoreCase) != -1) ||
                        (content.IndexOf("System.Web.Razor, Version=3", StringComparison.OrdinalIgnoreCase) != -1) ||
                        (content.IndexOf("Microsoft.AspNet.Mvc.5", StringComparison.OrdinalIgnoreCase) != -1))
                    {
                        // The project references Razor v3
                        razorRuntime = RazorRuntime.Version3;
                    }

                    return content.IndexOf("System.Web.Mvc", StringComparison.OrdinalIgnoreCase) != -1;
                }
            }
            catch
            {
            }
            return null;
        }

        private static void AddCatalogIfHostsDirectoryExists(AggregateCatalog catalog, string directory)
        {
            var extensionsDirectory = Path.GetFullPath(Path.Combine(directory, "RazorHosts"));
            if (Directory.Exists(extensionsDirectory))
            {
                catalog.Catalogs.Add(new DirectoryCatalog(extensionsDirectory));
            }
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs eventArgs)
        {
            var nameToResolve = new AssemblyName(eventArgs.Name);
            string path = Path.Combine(_assemblyDirectory, "v" + nameToResolve.Version.Major, nameToResolve.Name) + ".dll";
            if (File.Exists(path))
            {
                return Assembly.LoadFrom(path);
            }
            return null;
        }

        /// <remarks>
        /// Attempts to locate where the RazorGenerator.Core assembly is being loaded from. This allows us to locate the v1 and v2 assemblies and the corresponding 
        /// System.Web.* binaries
        /// Assembly.CodeBase points to the original location when the file is shadow copied, so we'll attempt to use that first.
        /// </remarks>
        private static string GetAssesmblyDirectory()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Uri uri;
            if (Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out uri) && uri.IsFile)
            {
                return Path.GetDirectoryName(uri.LocalPath);
            }
            return Path.GetDirectoryName(assembly.Location);
        }

        public void Dispose()
        {
            if (_catalog != null)
            {
                _catalog.Dispose();
            }

            if (_loadExtensions)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            }
        }
    }
}