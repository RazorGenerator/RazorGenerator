using System;

namespace SampleRazorHelperLibrary
{
    /// <summary>
    /// A very basic template base class which 
    /// implements the Razor template base class "contract"
    /// and simply writes to the Console
    /// </summary>
    public abstract class BasicSampleTemplateBaseClass
    {
        public abstract void Execute();

        protected void Write(object value)
        {
            Console.Write(value);
        }

        protected void WriteLiteral(object value)
        {
            Console.Write(value);
        }
    }
}
