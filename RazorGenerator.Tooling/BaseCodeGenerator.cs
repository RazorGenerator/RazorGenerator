
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
using RazorGenerator.Core;

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
            string fileContents;
            try
            {
                fileContents = this.GenerateFileContent(inputFileContent);
            }
            catch (InvalidOperationException exception) // hold on, what's so special about `InvalidOperationException`? We should have a custom exception type if we're doing anything ourselves.
            {
                _ = this.ReportGenerationError(exception, out string exceptionReport);
                
                // Write the error to the output file, as error details can be too complex for comfortable display in VS' Error List window:
                try
                {
                    const string contentsFmt = "/* Failed to generate file:\r\n\r\n{0}\r\n*/";
                    string exReportSafe = exceptionReport.Replace( "*/", "* /" ); // so it doesn't break the comment.
                    string errorContents = String.Format(provider: CultureInfo.CurrentCulture, format: contentsFmt, exReportSafe);
                    
                    return ConvertToUtf8BytesWithBom(errorContents, this.codeGeneratorProgress);
                }
                catch( Exception fallbackException )
                {
                    return this.ReportEncodingError( fallbackException );
                }
            }
            catch (Exception exception)
            {
                return this.ReportGenerationError( exception, exceptionReport:  out _ );
            }

            //

            try
            {
                byte[] encodedBytes = ConvertToUtf8BytesWithBom(fileContents, this.codeGeneratorProgress);
                return encodedBytes;
            }
            catch( Exception encodingException )
            {
                return this.ReportEncodingError( encodingException );
            }
        }

        private byte[] ReportGenerationError(Exception exception, out string exceptionReport)
        {
            exceptionReport = exception.ToString();

            const string messageFmt = @"Failed to generate file: {0}\r\n\r\n{1}";
            string message = String.Format(provider: CultureInfo.CurrentCulture, format: messageFmt, exception.Message, exceptionReport);
            this.codeGeneratorProgress.GeneratorErrorOrThrow(isWarning: false, level: 1, message: message, line: 0, column: 0);
            return null;
        }

        private byte[] ReportEncodingError( Exception encodingException )
        {
            const string messageFmt = @"File generated succesfully, but failed to encode file contents string to bytes: {0}\r\n\r\n{1}";
            string message = String.Format(provider: CultureInfo.CurrentCulture, format: messageFmt, encodingException.Message, encodingException.ToString());
            this.codeGeneratorProgress.GeneratorErrorOrThrow(isWarning: false, level: 1, message: message, line: 0, column: 0);
            return null;
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
        protected FileInfo InputFilePath
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

        private static readonly UTF8Encoding _utf8NoBom = new UTF8Encoding( encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true );

        protected static byte[] ConvertToUtf8BytesWithoutBom(string content, IVsGeneratorProgress codeGeneratorProgress)
        {
            return _utf8NoBom.GetBytes( s: content );
        }

        protected static byte[] ConvertToUtf8BytesWithBom(string content, IVsGeneratorProgress codeGeneratorProgress)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.text.utf8encoding.getbytecount
            // > the number of bytes in the preamble is not reflected in the value returned by the GetByteCount method.

            int contentBytesCount = Encoding.UTF8.GetByteCount(content);

            byte[] outputBytes;
            Int32  outputBytesIndex = 0;
            {
                // Get the preamble (byte-order mark) for our encoding
                byte[] preambleBytes = Encoding.UTF8.GetPreamble();

                outputBytes = new byte[preambleBytes.Length + contentBytesCount];
                Array.Copy( sourceArray: preambleBytes, sourceIndex: 0, destinationArray: outputBytes, destinationIndex: 0, length: preambleBytes.Length );

                outputBytesIndex = preambleBytes.Length;
            }

            int contentBytesWritten = Encoding.UTF8.GetBytes( s: content, charIndex: 0, charCount: content.Length, bytes: outputBytes, byteIndex: outputBytesIndex );
            if (contentBytesWritten != contentBytesCount)
            {
                string message = "Expected to write {0:N0} bytes but wrote {1:N0} bytes.".Fmt(contentBytesCount, contentBytesWritten);
#if DEBUG
                throw new InvalidOperationException(message: message);
#else
                // Be more tolerant in Release builds:
                if( codeGeneratorProgress != null )
                {
                    codeGeneratorProgress.GeneratorErrorOrThrow(isWarning: true, level: 2, message: message, line: 0, column: 0);
                }
                else
                {
                    throw new InvalidOperationException(message: message);
                }
#endif
            }

            // Return the concatenated output:
            return outputBytes;
        }

        /// <summary>The method that does the actual work of generating code given the input file</summary>
        /// <param name="inputFileContent">File contents as a string</param>
        /// <returns>The generated code file's contents as a <see cref="string"/> - or <see langword="null"/>. If <see langword="null"/> is returned then <see cref="IVsSingleFileGenerator.Generate(string, string, string, IntPtr[], out uint, IVsGeneratorProgress)"/> will return <see cref="VSConstants.E_FAIL"/>.</returns>
        protected abstract string GenerateFileContent(string inputFileContent);
    }
}
