/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj80;
using VsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.Web.RazorSingleFileGenerator {
    [ComVisible(true)]
    [Guid("BF3C7ABE-C85D-497F-952D-23DC8AA3CC43")]
    [CodeGeneratorRegistration(typeof(RazorHelperGenerator), "C# Razor Helper Generator (.cshtml)", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(RazorHelperGenerator))]
    public class RazorHelperGenerator : BaseCodeGeneratorWithSite {
        private static readonly ISet<string> _mvcProjectGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { 
            "{E53F8FEA-EAE0-44A6-8774-FFD645390401}" // MVC 3 Project Guid
        };

#pragma warning disable 0414
        //The name of this generator (use for 'Custom Tool' property of project item)
        internal static string name = "RazorHelperGenerator";
#pragma warning restore 0414

        /// <summary>
        /// Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected override byte[] GenerateCode(string inputFileContent) {
            var codeGenerator = new RazorCodeGenerator() { ErrorHandler = GeneratorError };
            if (this.CodeGeneratorProgress != null) {
                codeGenerator.CompletionHandler = CodeGeneratorProgress.Progress;
            }

            // Get the root folder of the project
            var appRoot = Path.GetDirectoryName(GetProject().FullName);

            // Determine the project-relative path
            string projectRelativePath = InputFilePath.Substring(appRoot.Length);

            CompiledWebHelperRazorHost razorHost;
            // Try and detect if the project is a MVC project. If so, use build a Mvc helper.
            if (ProjectIsMVC(GetProject())) {
                razorHost = new CompiledMvcHelperRazorHost(FileNameSpace, projectRelativePath, InputFilePath);
            }
            else {
                razorHost = new CompiledWebHelperRazorHost(FileNameSpace, projectRelativePath, InputFilePath);
            }

            return codeGenerator.GenerateCode(inputFileContent, razorHost, GetCodeProvider());
        }

        private bool ProjectIsMVC(Project project) {
            try {
                var projectTypeGUIDs = GetProjectTypeGUIDs(project);
                return projectTypeGUIDs.Any(g => _mvcProjectGuids.Contains(g));
            }
            catch {
                return false;
            }
        }

        private static IEnumerable<string> GetProjectTypeGUIDs(Project project) {
            IVsHierarchy hierarchy;
            IVsSolution solution = QueryService<IVsSolution>(project.DTE);

            int hr = solution.GetProjectOfUniqueName(project.UniqueName, out hierarchy);

            if (hr != 0) {
                Marshal.ThrowExceptionForHR(hr);
            }

            string projectTypeGuids;
            hr = ((IVsAggregatableProject)hierarchy).GetAggregateProjectTypeGuids(out projectTypeGuids);

            if (hr != 0) {
                Marshal.ThrowExceptionForHR(hr);
            }

            return projectTypeGuids.Split(';');
        }

        private static TService QueryService<TService>(_DTE dte) {
            Guid guidService = typeof(TService).GUID;
            Guid riid = typeof(TService).GUID;
            var serviceProvider = dte as VsServiceProvider;

            IntPtr servicePtr;
            int hr = serviceProvider.QueryService(ref guidService, ref riid, out servicePtr);

            if (hr != 0) {
                return default(TService);
            }

            object service = null;

            if (servicePtr != IntPtr.Zero) {
                service = Marshal.GetObjectForIUnknown(servicePtr);
                Marshal.Release(servicePtr);
            }

            return (TService)service;
        }
    }
}