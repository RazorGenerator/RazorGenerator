/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;
using RazorGenerator.Core;

namespace RazorGenerator
{
    /// <summary>
    /// This is the generator class. 
    /// When setting the 'Custom Tool' property of a C#, VB, or J# project item to "RazorGenerator", 
    /// the GenerateCode function will get called and will return the contents of the generated file 
    /// to the project system
    /// </summary>
    [ComVisible(true)]
    [Guid("52B316AA-1997-4c81-9969-83604C09EEB4")]
    [CodeGeneratorRegistration(typeof(RazorGenerator), "C# Razor Generator", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(RazorGenerator))]
    public class RazorGenerator : BaseCodeGeneratorWithSite
    {
#pragma warning disable 0414
        //The name of this generator (use for 'Custom Tool' property of project item)
        internal static string name = "RazorGenerator";
#pragma warning restore 0414

        /// <summary>
        /// Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected override byte[] GenerateCode(string inputFileContent)
        {
            try
            {
                return GenerateFromHost();
            }
            catch (InvalidOperationException exception)
            {
                GeneratorError(0, exception.Message, 0, 0);
                return ConvertToBytes(exception.Message);
            }
            catch (Exception exception)
            {
                GeneratorError(0, exception.Message, 0, 0);
            }
            return null;
        }

        private byte[] GenerateFromHost()
        {
            var projectDirectory = Path.GetDirectoryName(GetProject().FullName);
            var projectRelativePath = InputFilePath.Substring(projectDirectory.Length);

            using (var hostManager = new HostManager(projectDirectory))
            {
                var host = hostManager.CreateHost(InputFilePath, projectRelativePath, GetCodeProvider(), FileNameSpace);
                host.Error += (o, eventArgs) =>
                {
                    GeneratorError(0, eventArgs.ErrorMessage, eventArgs.LineNumber, eventArgs.ColumnNumber);
                };
                host.Progress += (o, eventArgs) =>
                {
                    if (CodeGeneratorProgress != null)
                    {
                        CodeGeneratorProgress.Progress(eventArgs.Completed, eventArgs.Total);
                    }
                };

                var content = host.GenerateCode();
                return ConvertToBytes(content);
            }
        }

        private static byte[] ConvertToBytes(string content)
        {
            //Get the preamble (byte-order mark) for our encoding
            byte[] preamble = Encoding.UTF8.GetPreamble();
            int preambleLength = preamble.Length;

            byte[] body = Encoding.UTF8.GetBytes(content);

            //Prepend the preamble to body (store result in resized preamble array)
            Array.Resize<byte>(ref preamble, preambleLength + body.Length);
            Array.Copy(body, 0, preamble, preambleLength, body.Length);

            //Return the combined byte array
            return preamble;
        }
    }
}