#define USE_IVsSingleFileGenerator

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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using RazorGenerator.Core;

namespace RazorGenerator
{
    internal static class VSGuids
    {
        public const String CSharpProjectGuid = @"{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}";
        public const String VBProjectGuid     = @"{164B10B9-B200-11D0-8C61-00A0C91E29D5}";
    }

    public class SomeBaseClass
        // Interesting: When `class RazorGenerator` has a base-class, VS will load and construct it - but won't call the interface methods, but why?
    {
        protected SomeBaseClass()
        {
            System.Diagnostics.Debug.WriteLine( "[RazorGenerator] SomeBaseClass.ctor" );
        }
    }

    /// <summary>This is the generator class.<br />
    /// When setting the 'Custom Tool' property of a C# or VB project item to &quot;RazorGenerator&quot; the <see cref="GenerateCode(string)"/> method will get called and will return the contents of the generated file to the project system</summary>
    
    [PackageRegistration( UseManagedResourcesOnly = true )]//, AllowsBackgroundLoading = true )]
    [InstalledProductRegistration( productName: "RazorGenerator.Tooling for VS2019", productDetails: "Razor-sharp!", productId: "2.0")] 
    
    [ComVisible(true)]
    [Guid("52B316AA-1997-4c81-9969-83604C09EEB4")]

    [CodeGeneratorRegistration(generatorType: typeof(RazorGenerator), generatorName: "C# Razor Generator"    , contextGuid: VSGuids.CSharpProjectGuid, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(generatorType: typeof(RazorGenerator), generatorName: "VB.NET Razor Generator", contextGuid: VSGuids.VBProjectGuid    , GeneratesDesignTimeSource = true)]
    
    [ProvideObject(typeof(RazorGenerator))]

    
    public sealed class RazorGenerator :
#if USE_IVsSingleFileGenerator
        SomeBaseClass,
        IVsSingleFileGenerator
#elif USE_BaseCodeGenerator
        BaseCodeGenerator
#elif USE_BaseCodeGeneratorWithSite
        BaseCodeGeneratorWithSite
#endif
    {
        //The name of this generator (use for 'Custom Tool' property of project item)
#pragma warning disable IDE1006 // Naming Styles. Keeping this field without an underscore prefix until I know it's safe to add it.
        internal static readonly string name = "RazorGenerator";
#pragma warning restore
        public RazorGenerator()
            : base()
        {
            System.Diagnostics.Debug.WriteLine( "[RazorGenerator] RazorGenerator.ctor" );
        }

#if USE_IVsSingleFileGenerator
        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            bool isUIThread = ThreadHelper.CheckAccess();

            pbstrDefaultExtension = ".xml";
            return pbstrDefaultExtension.Length;
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents,
          string wszDefaultNamespace, IntPtr[] rgbOutputFileContents,
          out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            bool isUIThread = ThreadHelper.CheckAccess();

            try
            {
                int lineCount = bstrInputFileContents.Split('\n').Length;
                byte[] bytes = Encoding.UTF8.GetBytes("<LineCount1>" + lineCount.ToString() + "</LineCount1>" );
                int length = bytes.Length;
                rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
                Marshal.Copy(bytes, 0, rgbOutputFileContents[0], length);
                pcbOutput = (uint)length;
            }
            catch (Exception ex)
            {
                pcbOutput = 0;
            }
            return VSConstants.S_OK;
        }

#elif USE_BaseCodeGenerator

        protected override byte[] GenerateCode(string inputFileContent)
        {
            try
            {
                int lineCount = inputFileContent.Split('\n').Length;
                byte[] bytes = Encoding.UTF8.GetBytes("<LineCount2>" + lineCount.ToString() + "</LineCount2>" );
                return bytes;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected override string GetDefaultExtension()
        {
            return ".xml";
        }

#elif USE_BaseCodeGeneratorWithSite

        /// <summary>
        /// Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected override byte[] GenerateCode(string inputFileContent)
        {


            return new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                return this.GenerateFromHost();
            }
            catch (InvalidOperationException exception)
            {
                this.GeneratorError(0, exception.Message, 0, 0);
                return ConvertToBytes(exception.Message);
            }
            catch (Exception exception)
            {
                this.GeneratorError(0, exception.Message, 0, 0);
            }

            return null;
        }

        private byte[] GenerateFromHost()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projectDirectory = Path.GetDirectoryName(this.GetProject().FullName);
            var projectRelativePath = this.InputFilePath.Substring(projectDirectory.Length);

            using (var hostManager = new HostManager(projectDirectory))
            {
                var host = hostManager.CreateHost(this.InputFilePath, projectRelativePath, this.GetCodeProvider(), this.FileNameSpace);
               
                host.Error += (o, eventArgs) =>
                {
                    this.GeneratorError(0, eventArgs.ErrorMessage, eventArgs.LineNumber, eventArgs.ColumnNumber);
                };

                host.Progress += (o, eventArgs) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();

                    if (this.CodeGeneratorProgress != null)
                    {
                        this.CodeGeneratorProgress.Progress(eventArgs.Completed, eventArgs.Total);
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

#endif
    }
}
