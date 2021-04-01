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
using RazorGenerator.Core.Hosting;

namespace RazorGenerator.Core
{
    public sealed class RazorHostManager : IDisposable
    {
        private readonly DirectoryInfo baseDirectory;
        private readonly bool          loadExtensions;
        private readonly FileInfo      razorGeneratorAssemblyFile;
        private readonly bool          useLatestVersion;

        private ComposablePartCatalog _catalog;

        // Commented out because whatever program code is responsible for hosting RazorHostManager should know how to fully configure it. And defaulting to Version1 is just silly.
//      public RazorHostManager(DirectoryInfo baseDirectory)
//          : this(baseDirectory, loadExtensions: true, defaultRuntime: RazorRuntime.Version1, assemblyDirectory: GetAssesmblyDirectory())
//      {
//
//      }
        
//      public RazorHostManager(DirectoryInfo baseDirectory, bool loadExtensions, RazorRuntime defaultRuntime)
//      {
//
//      }

        /// <summary>Loads RazorGenerator from a specific assembly. The first found class that implements <see cref="IRazorHostProvider"/> and is exported by <see cref="System.ComponentModel.Composition.ExportAttribute"/> will be used.</summary>
        /// <param name="baseDirectory"></param>
        /// <param name="loadExtensions"></param>
        /// <param name="razorGeneratorAssemblyFile"></param>
        public RazorHostManager(DirectoryInfo baseDirectory, bool loadExtensions, FileInfo razorGeneratorAssemblyFile)
        {
            this.baseDirectory              = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            this.loadExtensions             = loadExtensions;
            this.razorGeneratorAssemblyFile = razorGeneratorAssemblyFile ?? throw new ArgumentNullException(nameof(razorGeneratorAssemblyFile));
            this.useLatestVersion           = false;

            // Repurposing loadExtensions to mean unit-test scenarios. Don't bind to the AssemblyResolve in unit tests
            if (this.loadExtensions)
            {
                //AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
            }
        }

        public IRazorHost CreateHost(FileInfo razorFile, string projectRelativePath, string vsNamespace)
        {
            CodeLanguageUtil langutil = CodeLanguageUtil.GetLanguageUtilFromFileName(razorFile);

            using (CodeDomProvider codeDomProvider = langutil.GetCodeDomProvider())
            {
                return this.CreateHost( razorFile, projectRelativePath, codeDomProvider, vsNamespace );
            }
        }

        public IRazorHost CreateHost(FileInfo razorFile, string projectRelativePath, CodeDomProvider codeDomProvider, string vsNamespace)
        {
            #warning TODO: Cache this!
            Dictionary<string,string> inheritedDirectives = DirectivesParser.ReadInheritableDirectives( this.baseDirectory, razorFile );

            Dictionary<string, string> directives = DirectivesParser.ParseDirectives(razorFile, inheritedDirectives);
            directives["VsNamespace"] = vsNamespace;

            string guessedHost = null;
            RazorRuntime? runtime = null;
//          if (TryGuessHost(this.baseDirectory, projectRelativePath, out GuessedHost value))
//          {
//              runtime = value.Runtime;
//              guessedHost = value.Host;
//          }

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
                this._catalog = this.InitCompositionCatalog( baseDirectory: this.baseDirectory, loadExtensions: this.loadExtensions );//, runtime: runtime );
            }

            using (CompositionContainer container = new CompositionContainer(this._catalog))
            {
                IOutputRazorCodeTransformer codeTransformer = this.GetRazorCodeTransformer(container, projectRelativePath, hostName); // TODO: `hostName` should be `TemplateClass` or similar.

                IRazorHostProvider razorHostProvider = container.GetExport<IRazorHostProvider>().Value;

                if( razorHostProvider.CanGetRazorHost( out String errorDetails ) )
                {
                    return razorHostProvider.GetRazorHost(projectRelativePath, razorFile, codeTransformer, codeDomProvider, directives);
                }
                else
                {
                    const string fmt = "The current " + nameof(IRazorHostProvider) + " ({0}) cannot be used in the current environment:\r\n{1}";
                    string message = fmt.Fmt( razorHostProvider.GetType().FullName, errorDetails );
                    throw new InvalidOperationException( message );
                }
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
                throw new InvalidOperationException(message: RazorGeneratorResources.GeneratorFailureMessage.Fmt(projectRelativePath, availableHosts), innerException: exception);
            }

            if (codeTransformer is null)
            {
                throw new InvalidOperationException(message: RazorGeneratorResources.GeneratorError_UnknownGenerator.Fmt(hostName));
            }

            return codeTransformer;
        }

        private ComposablePartCatalog InitCompositionCatalog(DirectoryInfo baseDirectory, bool loadExtensions)//, RazorRuntime runtime)
        {
            // Retrieve available hosts
            Assembly hostsAssembly = Assembly.LoadFrom( this.razorGeneratorAssemblyFile.FullName );// this.GetAssembly(runtime);
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
    }
}
