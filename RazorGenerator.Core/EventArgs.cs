using System;

namespace RazorGenerator.Core
{
    public class GeneratorErrorEventArgs : EventArgs
    {
        public GeneratorErrorEventArgs(uint errorCode, string errorMessage, uint lineNumber, uint columnNumber)
        {
            this.ErorrCode    = errorCode;
            this.ErrorMessage = errorMessage;
            this.LineNumber   = lineNumber;
            this.ColumnNumber = columnNumber;
        }

        public uint ErorrCode { get; }

        /// <summary>Can be <see langword="null"/>.</summary>
        public string ErrorMessage { get; }

        public uint LineNumber { get; }

        public uint ColumnNumber { get; }
    }

    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(uint completed, uint total)
        {
            this.Completed = completed;
            this.Total     = total;
        }

        public uint Completed { get; private set; }

        public uint Total { get; private set; }
    }
}
