
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using RazorGenerator.Resources;

namespace RazorGenerator
{
    /// <summary>
    /// A managed wrapper for VS's concept of an IVsSingleFileGenerator which is
    /// a custom tool invoked at design time which can take any file as an input
    /// and provide any file as output.
    /// </summary>
    [ComVisible(true)]
    public abstract class BaseCodeGenerator : IVsSingleFileGenerator
    {
        private IVsGeneratorProgress codeGeneratorProgress;
        private string               codeFileNameSpace = String.Empty;
        private FileInfo             codeFilePath      = null;

        #region IVsSingleFileGenerator Members

       /// <summary>Implements the <c>IVsSingleFileGenerator.<see cref="IVsSingleFileGenerator.DefaultExtension"/></c> method.<br />
        /// Returns the extension of the generated file</summary>
        /// <param name="pbstrDefaultExtension">Out parameter, will hold the extension that is to be given to the output file name. The returned extension must include a leading period</param>
        /// <returns>S_OK if successful, E_FAIL if not</returns>
        int IVsSingleFileGenerator.DefaultExtension(out string pbstrDefaultExtension)
        {
            try
            {
                pbstrDefaultExtension = this.GetDefaultExtension();
                return VSConstants.S_OK;
            }
            catch (Exception e)
            {
                Trace.WriteLine(SingleFileResources.GetDefaultExtensionFailed);
                Trace.WriteLine(e.ToString());
                pbstrDefaultExtension = string.Empty;
                return VSConstants.E_FAIL;
            }
        }

        /// <summary>Implements the <c>IVsSingleFileGenerator.<see cref="IVsSingleFileGenerator.Generate"/></c> method.<br />
        /// Executes the transformation and returns the newly generated output file, whenever a custom tool is loaded, or the input file is saved</summary>
        /// <param name="wszInputFilePath">The full path of the input file. May be a null reference (Nothing in Visual Basic) in future releases of Visual Studio, so generators should not rely on this value</param>
        /// <param name="bstrInputFileContents">The contents of the input file. This is either a UNICODE BSTR (if the input file is text) or a binary BSTR (if the input file is binary). If the input file is a text file, the project system automatically converts the BSTR to UNICODE</param>
        /// <param name="wszDefaultNamespace">This parameter is meaningful only for custom tools that generate code. It represents the namespace into which the generated code will be placed. If the parameter is not a null reference (Nothing in Visual Basic) and not empty, the custom tool can use the following syntax to enclose the generated code</param>
        /// <param name="rgbOutputFileContents">(Byte array) [out] Returns an array of bytes to be written to the generated file. You must include UNICODE or UTF-8 signature bytes in the returned byte array, as this is a raw stream. The memory for rgbOutputFileContents must be allocated using the .NET Framework call, System.Runtime.InteropServices.AllocCoTaskMem, or the equivalent Win32 system call, CoTaskMemAlloc. The project system is responsible for freeing this memory</param>
        /// <param name="pcbOutput">[out] Returns the count of bytes in the rgbOutputFileContent array</param>
        /// <param name="pGenerateProgress">A reference to the IVsGeneratorProgress interface through which the generator can report its progress to the project system</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns E_FAIL</returns>
        int IVsSingleFileGenerator.Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            // `wszInputFilePath` can be null.
            if (bstrInputFileContents is null) throw new ArgumentNullException(nameof(bstrInputFileContents));
            // `wszDefaultNamespace` can be null.
            // `rgbOutputFileContents` is `out`.
            // `pcbOutput` is `out`.
            // `pGenerateProgress` can be null - or can it?

            //

            ThreadHelper.ThrowIfNotOnUIThread();

            this.codeFilePath          = new FileInfo(wszInputFilePath);
            this.codeFileNameSpace     = wszDefaultNamespace;
            this.codeGeneratorProgress = pGenerateProgress;

            byte[] bytes = this.TryGenerateOrNull(bstrInputFileContents);

            if (bytes is null)
            {
                // This signals that GenerateCode() has failed. Tasklist items have been put up in GenerateCode()
                rgbOutputFileContents[0] = IntPtr.Zero;
                pcbOutput = 0;

                // Return E_FAIL to inform Visual Studio that the generator has failed (so that no file gets generated)
                return VSConstants.E_FAIL;
            }
            else
            {
                // The contract between IVsSingleFileGenerator implementors and consumers is that 
                // any output returned from IVsSingleFileGenerator.Generate() is returned through  
                // memory allocated via CoTaskMemAlloc(). Therefore, we have to convert the 
                // byte[] array returned from GenerateCode() into an unmanaged blob.  

                int outputLength = bytes.Length;
                rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputLength);
                Marshal.Copy(bytes, 0, rgbOutputFileContents[0], outputLength);
                pcbOutput = (uint)outputLength;
                return VSConstants.S_OK;
            }
        }

        private byte[] TryGenerateOrNull(string inputFileContent)
        {
            try
            {
                string fileContents = this.GenerateFileContent(inputFileContent);
                return ConvertToUtf8Bytes(fileContents);
            }
            catch (InvalidOperationException exception)
            {
                string exReport = exception.ToString();

                {
                    const string messageFmt = "Failed to generate file: {0}\r\n\r\n{1}";
                    string message = String.Format(provider: CultureInfo.CurrentCulture, format: messageFmt, exception.Message, exReport);
                    this.codeGeneratorProgress.GeneratorErrorOrThrow(isWarning: false, level: 1, message: message, line: 0, column: 0);
                }
                
                {
                    const string contentsFmt = "/* Failed to generate file:\r\n\r\n{0}\r\n*/";
                    string exReportSafe = exReport.Replace( "*/", "* /" ); // so it doesn't break the comment.
                    string errorContents = String.Format(provider: CultureInfo.CurrentCulture, format: contentsFmt, exReportSafe);
                    
                    return ConvertToUtf8Bytes(errorContents);
                }
                
            }
            catch (Exception exception)
            {
                const string messageFmt = @"Failed to generate file: {0}\r\n\r\n{1}";
                string message = String.Format(provider: CultureInfo.CurrentCulture, format: messageFmt, exception.Message, exception.ToString());
                this.codeGeneratorProgress.GeneratorErrorOrThrow(isWarning: false, level: 1, message: message, line: 0, column: 0);
                return null;
            }
        }

        #endregion

        /// <summary>Namespace for the file</summary>
        protected string FileNameSpace
        {
            get
            {
                return this.codeFileNameSpace;
            }
        }

        /// <summary>File-path for the input file</summary>
        protected string InputFilePath
        {
            get
            {
                return this.codeFilePath;
            }
        }

        /// <summary>Interface to the VS shell object we use to tell our progress while we are generating. Will return <see langword="null"/> if this property is used outside of a <see cref="IVsSingleFileGenerator.Generate(string, string, string, IntPtr[], out uint, IVsGeneratorProgress)"/> call.</summary>
        internal IVsGeneratorProgress Progress
        {
            get
            {
                return this.codeGeneratorProgress;
            }
        }

        /// <summary>Gets the default extension for this generator</summary>
        /// <returns>String with the default extension for this generator</returns>
        protected abstract string GetDefaultExtension();

        protected static byte[] ConvertToUtf8Bytes(string content)
        {
            int contentBytesCount = Encoding.UTF8.GetByteCount(content);

            //Get the preamble (byte-order mark) for our encoding
            byte[] preambleBytes = Encoding.UTF8.GetPreamble();

            byte[] contentBytes = new byte[contentBytesCount + preambleBytes.Length];

            int contentBytesWritten = Encoding.UTF8.GetBytes(s: content, charIndex: 0, charCount: content.Length, bytes: contentBytes, byteIndex: preambleBytes.Length);
            if (contentBytesWritten != contentBytes.Length) throw new InvalidOperationException(message: "Expected to write {0:N0} bytes but wrote {1:N0} bytes.".Fmt(contentBytes.Length, contentBytesWritten));

            //Return the combined byte array
            return contentBytes;
        }

        /// <summary>The method that does the actual work of generating code given the input file</summary>
        /// <param name="inputFileContent">File contents as a string</param>
        /// <returns>The generated code file's contents as a <see cref="string"/> - or <see langword="null"/>. If <see langword="null"/> is returned then <see cref="IVsSingleFileGenerator.Generate(string, string, string, IntPtr[], out uint, IVsGeneratorProgress)"/> will return <see cref="VSConstants.E_FAIL"/>.</returns>
        protected abstract string GenerateFileContent(string inputFileContent);
    }
}
