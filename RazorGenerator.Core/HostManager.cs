using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
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
        private CompositionContainer _container;

        public HostManager(string baseDirectory)
            : this(baseDirectory, loadExtensions: true, defaultRuntime: RazorRuntime.Version1)
        {
        }

        public HostManager(string baseDirectory, bool loadExtensions, RazorRuntime defaultRuntime)
        {
            _loadExtensions = loadExtensions;
            _baseDirectory = baseDirectory;
            _defaultRuntime = defaultRuntime;
        }

        /// <summary>
        /// Work around for VS 2012's shadow copying
        /// </summary>
        internal static string AssemblyDirectory { get; set; }

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
            var codeTransformer = GetRazorCodeTransformer(projectRelativePath, directives);

            var host = _container.GetExport<IHostProvider>().Value;
            return host.GetRazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives);
        }

        private IRazorCodeTransformer GetRazorCodeTransformer(string projectRelativePath, IDictionary<string, string> directives)
        {
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
                runtime = razorVersion == "2" ? RazorRuntime.Version2 : RazorRuntime.Version1;
            }

            EnsureCompositionContainer(runtime);
            IRazorCodeTransformer codeTransformer = null;
            try
            {
                codeTransformer = _container.GetExportedValue<IRazorCodeTransformer>(hostName);
            }
            catch (Exception exception)
            {
                ThrowHostError(exception);
            }

            if (codeTransformer == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, RazorGeneratorResources.GeneratorError_UnknownGenerator, hostName));
            }
            return codeTransformer;
        }

        private void EnsureCompositionContainer(RazorRuntime runtime)
        {
            if (_container == null)
            {
                _container = InitCompositionContainer(_baseDirectory, _loadExtensions, runtime);
            }
        }

        private static CompositionContainer InitCompositionContainer(string baseDirectory, bool loadExtensions, RazorRuntime runtime)
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

            var container = new CompositionContainer(catalog);
            container.ComposeParts();
            return container;
        }

        private IEnumerable<string> GetAvailableHosts()
        {
            // We need for a way to figure out what the exporting type is. This could return arbitrary exports that are not ISingleFileGenerators
            return from part in _container.Catalog.Parts
                   from export in part.ExportDefinitions
                   where !String.IsNullOrEmpty(export.ContractName)
                   select export.ContractName;
        }

        private static Assembly GetAssembly(RazorRuntime runtime)
        {
            int runtimeValue = (int)runtime;
            string assemblyDirectory = AssemblyDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            // Look for the assembly at vX\RazorGenerator.vX.dll. If not, assume it is at RazorGenerator.vX.dll
            string assemblyName = "RazorGenerator.Core.v" + runtimeValue + ".dll";
            string hostFile = Path.Combine(assemblyDirectory, "v" + runtimeValue, assemblyName);
            hostFile = File.Exists(hostFile) ? hostFile : Path.Combine(assemblyDirectory, "v" + runtimeValue + ".dll");

            return Assembly.LoadFrom(hostFile);
        }

        internal static string GuessHost(string projectRoot, string projectRelativePath, out RazorRuntime runtime)
        {
            bool? isMvcProject = IsMvcProject(projectRoot, out runtime);
            var mvcHelperRegex = new Regex(@"(^|\\)Views(\\.*)+Helpers?", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            if (mvcHelperRegex.IsMatch(projectRelativePath))
            {
                return "MvcHelper";
            }
            var mvcViewRegex = new Regex(@"(^|\\)Views\\", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            if (mvcViewRegex.IsMatch(projectRelativePath))
            {
                return "MvcView";
            }
            if (Path.GetFileNameWithoutExtension(projectRelativePath).Contains("Helper") && isMvcProject.HasValue)
            {
                return isMvcProject.Value ? "MvcHelper" : "WebPagesHelper";
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
                    if (content.IndexOf("System.Web.Razor, Version=2.0.0.0", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        // The project references Razor v2
                        razorRuntime = RazorRuntime.Version2;
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

        private void ThrowHostError(Exception innerException = null)
        {
            string availableHosts = String.Join(", ", GetAvailableHosts());
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, RazorGeneratorResources.GeneratorFailureMessage, availableHosts), innerException);
        }

        public void Dispose()
        {
            if (_container != null)
            {
                _container.Dispose();
            }
        }
    }
}