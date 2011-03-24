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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Razor;
using Microsoft.VisualStudio.Shell;
using VSLangProj80;

namespace Microsoft.Web.RazorSingleFileGenerator {
    /// <summary>
    /// This is the generator class. 
    /// When setting the 'Custom Tool' property of a C#, VB, or J# project item to "XmlClassGenerator", 
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
        private static readonly Dictionary<string, Type> _razorHosts = GetRazorHosts();

        private static Dictionary<string, Type> GetRazorHosts() {
            return (from type in typeof(RazorGenerator).Assembly.GetExportedTypes()
                        where type.Namespace.Equals(typeof(RazorGenerator).Namespace + ".RazorHost", StringComparison.Ordinal)
                        select type).ToDictionary(p => p.Name.Replace("Host", null), StringComparer.Ordinal);
        }


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
            try {
                var generator = GetRazorHost();
                if (generator != null) {
                    return codeGenerator.GenerateCode(inputFileContent, generator, generator as IHostContext, GetCodeProvider());
                }
            }
            catch (Exception ex) {
                GenerateError(ex.Message);
            }
            return Encoding.UTF8.GetBytes(String.Format(
@" 
/***********************************************************************************************************
Could not precompile the input file contents. Ensure that a generator declaration exists in the cshtml file. 
A generator declaration is the first line of your cshtml file and looks like this: 
@* Generator: MvcHelper *@
Valid host names: {0}
************************************************************************************************************/
", String.Join(", ", _razorHosts.Keys)));
        }

        private RazorEngineHost GetRazorHost() {
            var generatorDeclaration = ReadGeneratorDeclaration();
            if (!String.IsNullOrEmpty(generatorDeclaration)) {
                var generatorName = ParseGeneratorDeclaration(generatorDeclaration);
                return InstantiateGenerator(generatorName);
            }
            return null;
        }

        private RazorEngineHost InstantiateGenerator(string generatorName) {
            // Generator name is the name of the RazorHost type without the suffix host e.g. Generator : WebPages
             
            Type hostType;
            if (!_razorHosts.TryGetValue(generatorName, out hostType)) {
                GenerateError(String.Format("Could not load generator \"{0}\".", generatorName));
                return null;
            }
            var constructor = hostType.GetConstructor(new[] { typeof(string), typeof(string), typeof(string) });
            return (RazorEngineHost)constructor.Invoke(new object[] { FileNameSpace, GetProjectRelativePath(), InputFilePath });
        }

        private string ParseGeneratorDeclaration(string declaration) {
            const string group = "generator";
            var regex = new Regex(String.Format(@"^@\*\s*Generator\s*:\s*(?<{0}>\w+)", group));
            var match = regex.Match(declaration);
            if (!match.Success) {
                GenerateError("No generator found");
                return null;
            }
            else {
                return match.Groups[group].Value;
            }
        }

        private string ReadGeneratorDeclaration() {
            try {
                using (var reader = new StreamReader(File.Open(InputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
                    return reader.ReadLine();
                }
            }
            catch (IOException exception) {
                GenerateError(exception.Message);
            }
            return null;
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
    }
}