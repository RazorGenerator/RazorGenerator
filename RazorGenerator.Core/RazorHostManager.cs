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

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    public sealed class RazorHostManager : IDisposable
    {
        private readonly DirectoryInfo baseDirectory;
        private readonly bool          loadExtensions;
        private readonly RazorRuntime  defaultRuntime;
        private readonly DirectoryInfo assemblyDirectory;

        private ComposablePartCatalog _catalog;

        public RazorHostManager(DirectoryInfo baseDirectory)
            : this(baseDirectory, loadExtensions: true, defaultRuntime: RazorRuntime.Version1, assemblyDirectory: GetAssesmblyDirectory())
        {

        }

        public RazorHostManager(DirectoryInfo baseDirectory, bool loadExtensions, RazorRuntime defaultRuntime, DirectoryInfo assemblyDirectory)
        {
            this.loadExtensions = loadExtensions;
            this.baseDirectory = baseDirectory;
            this.defaultRuntime = defaultRuntime;
            this.assemblyDirectory = assemblyDirectory;

            // Repurposing loadExtensions to mean unit-test scenarios. Don't bind to the AssemblyResolve in unit tests
            if (this.loadExtensions)
            {
                AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
            }
        }

        public IRazorHost CreateHost(FileInfo fullPath, string projectRelativePath, string vsNamespace)
        {
            CodeLanguageUtil langutil = CodeLanguageUtil.GetLanguageUtilFromFileName(fullPath);

            using (CodeDomProvider codeDomProvider = langutil.GetCodeDomProvider())
            {
                return this.CreateHost(fullPath, projectRelativePath, codeDomProvider, vsNamespace);
            }
        }

        public IRazorHost CreateHost(FileInfo fullPath, string projectRelativePath, CodeDomProvider codeDomProvider, string vsNamespace)
        {
            Dictionary<string, string> directives = DirectivesParser.ParseDirectives(this.baseDirectory, fullPath);
            directives["VsNamespace"] = vsNamespace;

            string guessedHost = null;
            RazorRuntime runtime = this.defaultRuntime;
            if (TryGuessHost(this.baseDirectory, projectRelativePath, out GuessedHost value))
            {
                runtime = value.Runtime;
                guessedHost = value.Host;
            }

            if (!directives.TryGetValue("Generator", out string hostName))
            {
                // Determine the host and runtime from the file \ project
                hostName = guessedHost;
            }

            if (directives.TryGetValue("RazorVersion", out string razorVersion))
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
                case "3":
                default:
                    runtime = RazorRuntime.Version3;
//                  throw new InvalidOperationException(); // TODO: How should users specify the default version?
                    break;
                }
            }

            if (this._catalog == null)
            {
                this._catalog = this.InitCompositionCatalog(this.baseDirectory, this.loadExtensions, runtime);
            }

            using (CompositionContainer container = new CompositionContainer(this._catalog))
            {
                IOutputRazorCodeTransformer codeTransformer = this.GetRazorCodeTransformer(container, projectRelativePath, hostName);
                IRazorHostProvider host = container.GetExport<IRazorHostProvider>().Value;
                return host.GetRazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives);
            }
        }

        private IOutputRazorCodeTransformer GetRazorCodeTransformer(CompositionContainer container, string projectRelativePath, string hostName)
        {
            IOutputRazorCodeTransformer codeTransformer;
            try
            {
                codeTransformer = container.GetExportedValue<IOutputRazorCodeTransformer>(hostName);
            }
            catch (ReflectionTypeLoadException)
            {
                throw;
            }
            catch (Exception exception)
            {
                string availableHosts = String.Join(", ", GetAvailableHosts(container));
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, RazorGeneratorResources.GeneratorFailureMessage, projectRelativePath, availableHosts), exception);
            }

            if (codeTransformer is null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, RazorGeneratorResources.GeneratorError_UnknownGenerator, hostName));
            }

            return codeTransformer;
        }

        private ComposablePartCatalog InitCompositionCatalog(DirectoryInfo baseDirectory, bool loadExtensions, RazorRuntime runtime)
        {
            // Retrieve available hosts
            Assembly hostsAssembly = this.GetAssembly(runtime);
            AggregateCatalog catalog = new AggregateCatalog(new AssemblyCatalog(hostsAssembly));

            if (loadExtensions)
            {
                // We assume that the baseDirectory points to the project root. Look for the RazorHosts directory under the project root
                AddCatalogIfHostsDirectoryExists(catalog, baseDirectory);

                // Look for the Razor Hosts directory up to two directories above the baseDirectory. Hopefully this should cover the solution root.
                string solutionDirectory = Path.Combine(baseDirectory.FullName, @"..\");
                AddCatalogIfHostsDirectoryExists(catalog, new DirectoryInfo(solutionDirectory));

                solutionDirectory = Path.Combine(baseDirectory.FullName, @"..\..\");
                AddCatalogIfHostsDirectoryExists(catalog, new DirectoryInfo(solutionDirectory));
            }

            return catalog;
        }

        private static IEnumerable<string> GetAvailableHosts(CompositionContainer container)
        {
            // We need for a way to figure out what the exporting type is. This could return arbitrary exports that are not ISingleFileGenerators
            return container.Catalog.Parts
                .SelectMany(part => part.ExportDefinitions)
                .Where(exportDef => !String.IsNullOrEmpty(exportDef.ContractName))
                .Select(exportDef => exportDef.ContractName);
        }

        private static void AddCatalogIfHostsDirectoryExists(AggregateCatalog catalog, DirectoryInfo directory)
        {
            string extensionsDirectory = Path.GetFullPath(Path.Combine(directory.FullName, "RazorHosts"));
            if (Directory.Exists(extensionsDirectory))
            {
                catalog.Catalogs.Add(new DirectoryCatalog(extensionsDirectory));
            }
        }

        public void Dispose()
        {
            if (this._catalog != null)
            {
                this._catalog.Dispose();
            }

            if (this.loadExtensions)
            {
                //AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
            }
        }

        

        /// <remarks>
        /// Attempts to locate where the RazorGenerator.Core assembly is being loaded from. This allows us to locate the v1 and v2 assemblies and the corresponding 
        /// System.Web.* binaries
        /// Assembly.CodeBase points to the original location when the file is shadow copied, so we'll attempt to use that first.
        /// </remarks>
        private static DirectoryInfo GetAssesmblyDirectory()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out Uri uri) && uri.IsFile)
            {
                return new DirectoryInfo(Path.GetDirectoryName(uri.LocalPath));
            }
            return new DirectoryInfo(Path.GetDirectoryName(assembly.Location));
        }
    }
}
