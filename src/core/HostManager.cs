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

namespace RazorGenerator.Core {
    public class HostManager : IDisposable {
        private readonly CompositionContainer _container;

        public HostManager(string baseDirectory)
            : this(baseDirectory, loadExtensions : false) {
        }

        public HostManager(string baseDirectory, bool loadExtensions) {
            _container = InitCompositionContainer(baseDirectory, loadExtensions);
        }

        private IEnumerable<string> GetAvailableHosts() {
            // We need for a way to figure out what the exporting type is. This could return arbitrary exports that are not ISingleFileGenerators
            return from part in _container.Catalog.Parts
                   from export in part.ExportDefinitions
                   where !String.IsNullOrEmpty(export.ContractName)
                   select export.ContractName;
        }

        public RazorHost CreateHost(string fullPath, string projectRelativePath) {
            var codeDomProvider = new CSharpCodeProvider();
            return CreateHost(fullPath, projectRelativePath, codeDomProvider);
        }

        public RazorHost CreateHost(string fullPath, string projectRelativePath, CodeDomProvider codeDomProvider) {
            var directives = ParseDirectives(fullPath);
            var codeTransformer = GetRazorCodeTransformer(directives);
            return new RazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives);
        }

        private IDictionary<string, string> ParseDirectives(string filePath) {
            var inputFileContent = File.ReadAllText(filePath);
            int index = inputFileContent.IndexOf("*@", StringComparison.OrdinalIgnoreCase);
            var directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (inputFileContent.TrimStart().StartsWith("@*") && index != -1) {
                string directivesLine = inputFileContent.Substring(0, index).TrimStart('*', '@');

                var regex = new Regex(@"\b(?<Key>\w+)\s*:\s*(?<Value>\w+)\b");
                foreach (Match item in regex.Matches(directivesLine)) {
                    var key = item.Groups["Key"].Value;
                    var value = item.Groups["Value"].Value;

                    directives.Add(key, value);
                }
            }
            return directives;
        }

        private IRazorCodeTransformer GetRazorCodeTransformer(IDictionary<string, string> directives) {
            string hostName;
            if (!directives.TryGetValue("Generator", out hostName)) {
                string availableHosts = String.Join(", ", GetAvailableHosts());
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, RazorGeneratorResources.GeneratorFailureMessage, availableHosts));
            }

            var codeTransformer = _container.GetExportedValue<IRazorCodeTransformer>(hostName);
            if (codeTransformer == null) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, RazorGeneratorResources.GeneratorError_UnknownGenerator, hostName));
            }
            return codeTransformer;
        }

        private static CompositionContainer InitCompositionContainer(string baseDirectory, bool loadExtensions) {
            // Retrieve available hosts
            var catalog = new AggregateCatalog(new AssemblyCatalog(typeof(HostManager).Assembly));

            if (loadExtensions) {
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

        private static void AddCatalogIfHostsDirectoryExists(AggregateCatalog catalog, string directory) {
            var extensionsDirectory = Path.GetFullPath(Path.Combine(directory, "RazorHosts"));
            if (Directory.Exists(extensionsDirectory)) {
                catalog.Catalogs.Add(new DirectoryCatalog(extensionsDirectory));
            }
        }

        public void Dispose() {
            if (_container != null) {
                _container.Dispose();
            }
        }
    }
}