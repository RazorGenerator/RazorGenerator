
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Globalization;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace RazorGenerator
{
    public static class VsGeneratorProgressExtensions
    {
        /// <summary>Method that will communicate a warning or error via the shell callback mechanism</summary>
        /// <param name="progress">Instance of <see cref="IVsGeneratorProgress"/> to use. If this parameter is <see langword="null"/> this method does nothing and immediately returns.</param>
        /// <param name="level">Level or severity</param>
        /// <param name="message">Text displayed to the user</param>
        /// <param name="line">Line number of error</param>
        /// <param name="column">Column number of error</param>
        /// <exception cref="RazorGeneratorToolingException">Thrown if <paramref name="progress"/> is not <see langword="null"/> and if <see cref="IVsGeneratorProgress.GeneratorError(int, uint, string, uint, uint)"/> returns a value other than <see cref="VSConstants.S_OK"/>.</exception>
        /// <exception cref="COMException">Thrown if <paramref name="progress"/> is not <see langword="null"/> and if this method is called on a thread besides the UI thread. The thrown <see cref="COMException"/> will have error code <c>RPC_E_WRONG_THREAD</c>.</exception>
        public static void GeneratorErrorOrThrow(this IVsGeneratorProgress progress, Boolean isWarning, UInt32 level, String message, UInt32 line, UInt32 column)
        {
            // TODO: Report error/warning to Trace too?

            if (progress is null) return;

            ThreadHelper.ThrowIfNotOnUIThread();

            int fWarning = isWarning ? 1 : 0;

            int result = progress.GeneratorError(fWarning: fWarning, dwLevel: level, bstrError: message, dwLine: line, dwColumn: column);
            if (result != VSConstants.S_OK)
            {
                const string exMessageFmt = nameof(IVsGeneratorProgress) + "." + nameof(progress.GeneratorError) + "(fWarning: {0:D}, dwLevel: {1:D}, bstrError: {2}, dwLine: {3}, dwColumn: {4}) failed with error code {5:D} (0x{5:X8}).";
                string exMessage = string.Format(provider: CultureInfo.CurrentCulture, format: exMessageFmt, fWarning, level, message, line, column, result);
                throw new RazorGeneratorToolingException(message: exMessage, errorCode: result);
            }
        }

        /// <summary>Method that will communicate a progress figure via the shell callback mechanism</summary>
        /// <param name="progress">Instance of <see cref="IVsGeneratorProgress"/> to use. If this parameter is <see langword="null"/> this method does nothing and immediately returns.</param>
        /// <param name="nComplete">Index that specifies how much of the generation has been completed. This value can range from zero to <paramref name="nTotal"/>.</param>
        /// <param name="nTotal">The maximum value for <paramref name="nComplete"/>.</param>
        /// <exception cref="RazorGeneratorToolingException">Thrown if <paramref name="progress"/> is not <see langword="null"/> and if <see cref="IVsGeneratorProgress.Progress(uint, uint)"/> returns a value other than <see cref="VSConstants.S_OK"/>.</exception>
        /// <exception cref="COMException">Thrown if <paramref name="progress"/> is not <see langword="null"/> and if this method is called on a thread besides the UI thread. The thrown <see cref="COMException"/> will have error code <c>RPC_E_WRONG_THREAD</c>.</exception>
        public static void ProgressOrThrow(this IVsGeneratorProgress progress, UInt32 nComplete, UInt32 nTotal)
        {
            if (progress is null) return;

            ThreadHelper.ThrowIfNotOnUIThread();

            int result = progress.Progress(nComplete: nComplete, nTotal: nTotal);
            if (result != VSConstants.S_OK)
            {
                const string exMessageFmt = nameof(IVsGeneratorProgress) + "." + nameof(progress.Progress) + "(nComplete: {0:D}, nTotal: {1:D}) failed with error code {2:D} (0x{2:X8}).";
                string exMessage = string.Format(provider: CultureInfo.CurrentCulture, format: exMessageFmt, nComplete, nTotal, result);
                throw new RazorGeneratorToolingException(message: exMessage, errorCode: result);
            }
        }
    }
}
