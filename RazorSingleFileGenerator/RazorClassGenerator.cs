/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.WebPages.Razor;
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
    [CodeGeneratorRegistration(typeof(RazorClassGenerator), "C# Razor Generator (.cshtml)", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    //[CodeGeneratorRegistration(typeof(RazorClassGenerator), "VB Razor Generator (.vbhtml)", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(RazorClassGenerator))]
    public class RazorClassGenerator : BaseCodeGeneratorWithSite {
#pragma warning disable 0414
        //The name of this generator (use for 'Custom Tool' property of project item)
        internal static string name = "RazorClassGenerator";
#pragma warning restore 0414

        /// <summary>
        /// Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected override byte[] GenerateCode(string inputFileContent) {
            // Get the root folder of the project
            var appRoot = Path.GetDirectoryName(GetProject().FullName);

            // Determine the project-relative path
            string projectRelativePath = InputFilePath.Substring(appRoot.Length);

            // Turn it into a virtual path by prepending ~ and fixing it up
            string virtualPath = VirtualPathUtility.ToAppRelative("~" + projectRelativePath);

            // Create the same type of Razor host that's used to process Razor files in App_Code
            var host = new WebCodeRazorHost(virtualPath, InputFilePath);

            // Set the namespace to be the same as what's used by default for regular .cs files
            host.DefaultNamespace = FileNameSpace;

            // Create a Razor engine nad pass it our host
            var engine = new RazorTemplateEngine(host);

            // Generate code
            GeneratorResults results = null;
            try {
                using (TextReader reader = new StringReader(inputFileContent)) {
                    results = engine.GenerateCode(reader);
                }
            }
            catch (Exception e) {
                this.GeneratorError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }

            // Output errors
            foreach (RazorError error in results.ParserErrors) {
                GeneratorError(4, error.Message, (uint)error.Location.LineIndex + 1, (uint)error.Location.CharacterIndex + 1);
            }

            CodeDomProvider provider = GetCodeProvider();

            try {
                if (this.CodeGeneratorProgress != null) {
                    //Report that we are 1/2 done
                    this.CodeGeneratorProgress.Progress(50, 100);
                }

                using (StringWriter writer = new StringWriter(new StringBuilder())) {
                    CodeGeneratorOptions options = new CodeGeneratorOptions();
                    options.BlankLinesBetweenMembers = false;
                    options.BracingStyle = "C";

                    // Add a GeneratedCode attribute to the generated class
                    CodeCompileUnit generatedCode = results.GeneratedCode;
                    var ns = generatedCode.Namespaces[0];
                    CodeTypeDeclaration generatedType = ns.Types[0];
                    generatedType.CustomAttributes.Add(
                        new CodeAttributeDeclaration(
                            new CodeTypeReference(typeof(GeneratedCodeAttribute)),
                            new CodeAttributeArgument(new CodePrimitiveExpression("RazorSingleFileGenerator")),
                            new CodeAttributeArgument(new CodePrimitiveExpression("1.0"))));

                    // Remove all the WebMatrix namespaces
                    var imports = ns.Imports.OfType<CodeNamespaceImport>().Where(import => !import.Namespace.Contains("WebMatrix")).ToList();
                    ns.Imports.Clear();
                    imports.ForEach(import => ns.Imports.Add(import));

                    //Generate the code
                    provider.GenerateCodeFromCompileUnit(generatedCode, writer, options);

                    if (this.CodeGeneratorProgress != null) {
                        //Report that we are done
                        this.CodeGeneratorProgress.Progress(100, 100);
                    }
                    writer.Flush();

                    // Save as UTF8
                    Encoding enc = Encoding.UTF8;

                    //Get the preamble (byte-order mark) for our encoding
                    byte[] preamble = enc.GetPreamble();
                    int preambleLength = preamble.Length;

                    //Convert the writer contents to a byte array
                    byte[] body = enc.GetBytes(writer.ToString());

                    //Prepend the preamble to body (store result in resized preamble array)
                    Array.Resize<byte>(ref preamble, preambleLength + body.Length);
                    Array.Copy(body, 0, preamble, preambleLength, body.Length);

                    //Return the combined byte array
                    return preamble;
                }
            }
            catch (Exception e) {
                this.GeneratorError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }
        }
    }
}