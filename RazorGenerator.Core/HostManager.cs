using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace RazorGenerator.Core
{
    public class HostManager : IDisposable
    {
        private readonly CompositionContainer _container;
        private readonly string _baseDirectory;

        public HostManager(string baseDirectory)
            : this(baseDirectory, loadExtensions: true)
        {
        }

        public HostManager(string baseDirectory, bool loadExtensions)
        {
            _container = InitCompositionContainer(baseDirectory, loadExtensions);
            _baseDirectory = baseDirectory;
        }

        private IEnumerable<string> GetAvailableHosts()
        {
            // We need for a way to figure out what the exporting type is. This could return arbitrary exports that are not ISingleFileGenerators
            return from part in _container.Catalog.Parts
                   from export in part.ExportDefinitions
                   where !String.IsNullOrEmpty(export.ContractName)
                   select export.ContractName;
        }

        public RazorHost CreateHost(string fullPath, string projectRelativePath)
        {
            using (var codeDomProvider = new CSharpCodeProvider())
            {
                return CreateHost(fullPath, projectRelativePath, codeDomProvider);
            }
        }

        public RazorHost CreateHost(string fullPath, string projectRelativePath, CodeDomProvider codeDomProvider)
        {
            var directives = DirectivesParser.ParseDirectives(_baseDirectory, fullPath);
            var hostName = this.GetHostName(projectRelativePath, directives);
            var codeTransformer = GetRazorCodeTransformer(hostName);
            return new RazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives, hostName);
        }
        private string GetHostName(string projectRelativePath, IDictionary<string, string> directives)
        {
            string hostName;

            if (!directives.TryGetValue("Generator", out hostName))
            {
                hostName = GuessHost(_baseDirectory, projectRelativePath);
            }
            return hostName;
        }

        private IRazorCodeTransformer GetRazorCodeTransformer(string hostName)
        {
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

        private static CompositionContainer InitCompositionContainer(string baseDirectory, bool loadExtensions)
        {
            // Retrieve available hosts
            var catalog = new AggregateCatalog(new AssemblyCatalog(typeof(HostManager).Assembly));

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

        internal static string GuessHost(string projectRoot, string projectRelativePath)
        {
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
            if (Path.GetFileNameWithoutExtension(projectRelativePath).Contains("Helper"))
            {
                bool? isMvcProject = IsMvcProject(projectRoot);
                if (isMvcProject.HasValue)
                {
                    return isMvcProject.Value ? "MvcHelper" : "WebPagesHelper";
                }
            }
            return null;
        }

        private static bool? IsMvcProject(string projectRoot)
        {
            try
            {
                var projectFile = Directory.EnumerateFiles(projectRoot, "*.csproj").FirstOrDefault();
                if (projectFile != null)
                {
                    var content = File.ReadAllText(projectFile);
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