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
using System.IO;
using System.Runtime.InteropServices;

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

        public const String RazorGeneratorClsid = "52B316AA-1997-4c81-9969-83604C09EEB4";
    }

    /// <summary>This is the generator class.<br />
    /// When setting the 'Custom Tool' property of a C# or VB project item to &quot;RazorGenerator&quot; the <see cref="GenerateCode(string)"/> method will get called and will return the contents of the generated file to the project system</summary>
    
    [PackageRegistration( UseManagedResourcesOnly = true, AllowsBackgroundLoading = true )]
    [InstalledProductRegistration( productName: "RazorGenerator.Tooling for VS2019", productDetails: "Razor-sharp!", productId: "2.0")] 
    
    [ComVisible(true)] // IMPORTANT NOTE: `RazorGenerator` *and* every type that `RazorGenerator` inherits from must have the `[ComVisible(true)]` attribute applied!
    [Guid(VSGuids.RazorGeneratorClsid)]

    [CodeGeneratorRegistration(generatorType: typeof(RazorGenerator), generatorName: "C# Razor Generator"    , contextGuid: VSGuids.CSharpProjectGuid, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(generatorType: typeof(RazorGenerator), generatorName: "VB.NET Razor Generator", contextGuid: VSGuids.VBProjectGuid    , GeneratesDesignTimeSource = true)]
    
    [ProvideObject(typeof(RazorGenerator))]
    
    public sealed class RazorGenerator : BaseCodeGeneratorWithSite
    {
        //The name of this generator (use for 'Custom Tool' property of project item)
#pragma warning disable IDE1006 // Naming Styles. Keeping this field without an underscore prefix until I know it's safe to add it.
        internal static readonly string name = "RazorGenerator";
#pragma warning restore
        public RazorGenerator()
            : base()
        {
            Trace.WriteLine( "[RazorGenerator] RazorGenerator.ctor" );
        }

        /// <summary>The method that does the actual work of generating code given the input file</summary>
        /// <param name="inputFileContent">File contents as a string</param>
        /// <returns>The generated code file's contents as a <see cref="string"/> - or <see langword="null"/>. If <see langword="null"/> is returned then <see cref="IVsSingleFileGenerator.Generate(string, string, string, IntPtr[], out uint, IVsGeneratorProgress)"/> will return <see cref="VSConstants.E_FAIL"/>.</returns>
        protected sealed override string GenerateFileContent(string inputFileContent)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DirectoryInfo projectDirectory    = new DirectoryInfo(Path.GetDirectoryName(this.GetProject().FullName));
            string        projectRelativePath = this.InputFilePath.FullName.Substring(projectDirectory.FullName.Length);

            using (HostManager hostManager = new HostManager(projectDirectory))
            {
                IRazorHost host = hostManager.CreateHost(this.InputFilePath, projectRelativePath, this.GetCodeProvider(), this.FileNameSpace);
                
                try
                {
                    host.Error    += this.OnRazorHostError;
                    host.Progress += this.OnRazorHostProgress;

                    //

                    string content = host.GenerateCode();
                    return content;
                }
                finally
                {
                    host.Progress -= this.OnRazorHostProgress;
                    host.Error    -= this.OnRazorHostError;
                }
            }
        }

        private void OnRazorHostError(object sender, GeneratorErrorEventArgs e)
        {
            this.Progress?.GeneratorErrorOrThrow(isWarning: false, level: 0, message: e.ErrorMessage, line: e.LineNumber, column: e.ColumnNumber);
        }

        private void OnRazorHostProgress(object sender, ProgressEventArgs e)
        {
            this.Progress?.ProgressOrThrow(nComplete: e.Completed, nTotal: e.Total);
        }
    }
}
