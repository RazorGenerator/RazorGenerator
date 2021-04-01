using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    public interface IRazorHostProvider
    {
        /// <summary>Indicates if this <see cref="IRazorHostProvider"/> can run in the current AppDomain/process/environment.</summary>
        Boolean CanGetRazorHost( out String errorDetails );

        IRazorHost GetRazorHost(
            string                      projectRelativePath, 
            FileInfo                    fullPath, 
            IOutputRazorCodeTransformer codeTransformer,
            CodeDomProvider             codeDomProvider, 
            IDictionary<string,string>  directives
        );
    }

    public static class ReflectionExtensions
    {
        public static Boolean VersionIsInRange( this Assembly assembly, Version inclusiveLowerBound, Version exclusiveUpperBound, out String errorMessage )
        {
            if (assembly            is null) throw new ArgumentNullException(nameof(assembly));
            if (inclusiveLowerBound is null) throw new ArgumentNullException(nameof(inclusiveLowerBound));
            if (exclusiveUpperBound is null) throw new ArgumentNullException(nameof(exclusiveUpperBound));

            if( exclusiveUpperBound <= inclusiveLowerBound ) throw new ArgumentOutOfRangeException( paramName: nameof(exclusiveUpperBound), actualValue: exclusiveUpperBound, message: "Exclusive upper-bound version cannot be less-than or equal-to the inclusive lower-bound version." );

            //

            Version assemblyVersion = assembly.GetName().Version;

            if( inclusiveLowerBound <= assemblyVersion )
            {
                if( assemblyVersion < exclusiveUpperBound )
                {
                    errorMessage = null;
                    return true;
                }
                else
                {
                    errorMessage = "Assembly {0}'s version ({1}) is not less than the exclusive upper-bound version {2}.".Fmt( assembly.Location, assemblyVersion, exclusiveUpperBound );
                    return false;
                }
            }
            else
            {
                errorMessage = "Assembly {0}'s version ({1}) is lower than the required minimum version {2}.".Fmt( assembly.Location, assemblyVersion, inclusiveLowerBound );
                return false;
            }
        }
    }
}
