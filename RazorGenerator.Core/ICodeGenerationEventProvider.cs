using System;

namespace RazorGenerator.Core
{
    public interface ICodeGenerationEventProvider
    {
        event EventHandler<GeneratorErrorEventArgs> Error;

        event EventHandler<ProgressEventArgs> Progress;
    }
}
