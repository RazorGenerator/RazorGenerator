using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace RazorGenerator
{
    [Serializable]
    public class RazorGeneratorToolingException : COMException
    {
//        public RazorGeneratorToolingException()
//        {
//        }
//       
//        public RazorGeneratorToolingException(string message)
//            : base(message)
//        {
//        }
//        
//        public RazorGeneratorToolingException(string message, Exception inner)
//            : base(message, inner)
//        {
//        }
        
        protected RazorGeneratorToolingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        //

        public RazorGeneratorToolingException(string message, int errorCode)
            : base(message, errorCode)
        {
            this.VSErrorCode = errorCode;
        }

        public int VSErrorCode { get; internal set; }
    }
}
