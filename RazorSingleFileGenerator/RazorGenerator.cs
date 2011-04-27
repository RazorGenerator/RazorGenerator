/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.RazorSingleFileGenerator.Resources;
using VSLangProj80;

namespace Microsoft.Web.RazorSingleFileGenerator {
    /// <summary>
    /// This is the generator class. 
    /// When setting the 'Custom Tool' property of a C#, VB, or J# project item to "RazorGenerator", 
    /// the GenerateCode function will get called and will return the contents of the generated file 
    /// to the project system
    /// </summary>
    [ComVisible(true)]
    [Guid("52B316AA-1997-4c81-9969-83604C09EEB4")]
    [CodeGeneratorRegistration(typeof(RazorGenerator), "C# Razor Generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(RazorGenerator))]
    public class RazorGenerator : BaseCodeGeneratorWithSite {
#pragma warning disable 0414
        //The name of this generator (use for 'Custom Tool' property of project item)
        internal static string name = "RazorGenerator";
#pragma warning restore 0414

        /// <summary>
        /// Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected override byte[] GenerateCode(string inputFileContent) {
            var codeGenerator = new RazorCodeGenerator() { ErrorHandler = GeneratorError };
            if (this.CodeGeneratorProgress != null) {
                codeGenerator.CompletionHandler = CodeGeneratorProgress.Progress;
            }

            using (var compositionContainer = InitCompositionContainer()) {
                try {

                    var directives = ParseDirectives(inputFileContent);
                    RazorEngineHost host = GetRazorHost(compositionContainer, directives);
                    if (host != null) {
                        ISingleFileGenerator generator = (ISingleFileGenerator)host;
                        generator.PreCodeGeneration(codeGenerator, directives);
                        return codeGenerator.GenerateCode(inputFileContent, host, GetCodeProvider());
                    }

                }
                catch (Exception ex) {
                    GenerateError(ex.Message);
                }

                var availableHosts = GetAvailableHosts(compositionContainer);
                return Encoding.UTF8.GetBytes(String.Format(SingleFileResources.GeneratorFailureMessage,
                    String.Join(", ", availableHosts)));
            }
        }

        private IDictionary<string, string> ParseDirectives(string inputFileContent) {
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

        private RazorEngineHost GetRazorHost(CompositionContainer container, IDictionary<string, string> directives) {
            string hostName;
            if (!directives.TryGetValue("Generator", out hostName)) {
                return null;
            }

            var host = container.GetExportedValue<ISingleFileGenerator>(hostName);

            if (host == null) {
                GenerateError(String.Format(CultureInfo.CurrentCulture, SingleFileResources.GeneratorError_UnknownGenerator, hostName));
                return null;
            }
            return (RazorEngineHost)host;
        }

        private CompositionContainer InitCompositionContainer() {
            // Retrieve available hosts
            var catalog = new AggregateCatalog(new AssemblyCatalog(GetType().Assembly));

            // Add the folder RazorHosts under the project root
            var projectDirectory = Path.GetDirectoryName(GetProject().FullName);
            AddCatalogIfDirectoryExists(catalog, projectDirectory);

            // Add RazorHosts directory under the solution
            var solutionDirectory = Path.GetDirectoryName(GetProject().DTE.Solution.FullName);
            AddCatalogIfDirectoryExists(catalog, solutionDirectory);

            var container = new CompositionContainer(catalog);
            container.ComposeExportedValue("fileNamespace", FileNameSpace);
            container.ComposeExportedValue("projectRelativePath", GetProjectRelativePath());
            container.ComposeExportedValue("fullPath", InputFilePath);
            container.ComposeParts();

            return container;
        }

        private static void AddCatalogIfDirectoryExists(AggregateCatalog catalog, string directory) {
            var extensionsDirectory = Path.Combine(directory, "RazorHosts");
            if (Directory.Exists(extensionsDirectory)) {
                catalog.Catalogs.Add(new DirectoryCatalog(extensionsDirectory));
            }
        }

        private void GenerateError(string message) {
            GeneratorError(4, message, 0, 0);
        }

        private string GetProjectRelativePath() {
            // Get the root folder of the project
            var appRoot = Path.GetDirectoryName(GetProject().FullName);

            // Determine the project-relative path
            return InputFilePath.Substring(appRoot.Length);
        }

        private IEnumerable<string> GetAvailableHosts(CompositionContainer container) {
            // We need for a way to figure out what the exporting type is. This could return arbitrary exports that are not ISingleFileGenerators
            return from part in container.Catalog.Parts
                   from export in part.ExportDefinitions
                   where !String.IsNullOrEmpty(export.ContractName)
                   select export.ContractName;
        }
    }
}