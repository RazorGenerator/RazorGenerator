
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;
using VSOLE = Microsoft.VisualStudio.OLE.Interop;

namespace RazorGenerator
{
    /// <summary>
    /// Base code generator with site implementation
    /// </summary>
    public abstract class BaseCodeGeneratorWithSite : BaseCodeGenerator, VSOLE.IObjectWithSite
    {
        private object site = null;
        private CodeDomProvider codeDomProvider = null;
        private ServiceProvider serviceProvider = null;

        #region IObjectWithSite Members

        /// <summary>
        /// GetSite method of IOleObjectWithSite
        /// </summary>
        /// <param name="riid">interface to get</param>
        /// <param name="ppvSite">IntPtr in which to stuff return value</param>
        void VSOLE.IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            if (this.site == null)
            {
                throw new COMException("object is not sited", VSConstants.E_FAIL);
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            IntPtr pUnknownPointer = Marshal.GetIUnknownForObject(this.site);
            IntPtr intPointer = IntPtr.Zero;
            Marshal.QueryInterface(pUnknownPointer, ref riid, out intPointer);

            if (intPointer == IntPtr.Zero)
            {
                throw new COMException("site does not support requested interface", VSConstants.E_NOINTERFACE);
            }

            ppvSite = intPointer;
        }

        /// <summary>
        /// SetSite method of IOleObjectWithSite
        /// </summary>
        /// <param name="pUnkSite">site for this object to use</param>
        void VSOLE.IObjectWithSite.SetSite(object pUnkSite)
        {
            this.site = pUnkSite;
            this.codeDomProvider = null;
            this.serviceProvider = null;
        }

        #endregion

        /// <summary>
        /// Demand-creates a ServiceProvider
        /// </summary>
        private ServiceProvider SiteServiceProvider
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (this.serviceProvider == null)
                {
                    this.serviceProvider = new ServiceProvider(this.site as VSOLE.IServiceProvider);
                    Debug.Assert(this.serviceProvider != null, "Unable to get ServiceProvider from site object.");
                }
                return this.serviceProvider;
            }
        }

        /// <summary>
        /// Method to get a service by its GUID
        /// </summary>
        /// <param name="serviceGuid">GUID of service to retrieve</param>
        /// <returns>An object that implements the requested service</returns>
        protected object GetService(Guid serviceGuid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return this.SiteServiceProvider.GetService(serviceGuid);
        }

        /// <summary>
        /// Method to get a service by its Type
        /// </summary>
        /// <param name="serviceType">Type of service to retrieve</param>
        /// <returns>An object that implements the requested service</returns>
        protected object GetService(Type serviceType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return this.SiteServiceProvider.GetService(serviceType);
        }

        /// <summary>
        /// Returns a CodeDomProvider object for the language of the project containing
        /// the project item the generator was called on
        /// </summary>
        /// <returns>A CodeDomProvider object</returns>
        protected virtual CodeDomProvider GetCodeProvider()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.codeDomProvider == null)
            {
                // Query for `IVSMDCodeDomProvider` / `SVSMDCodeDomProvider` for this project type
                if (this.GetService(typeof(SVSMDCodeDomProvider)) is IVSMDCodeDomProvider provider)
                {
                    this.codeDomProvider = provider.CodeDomProvider as CodeDomProvider;
                }
                else
                {
                    //In the case where no language specific CodeDom is available, fall back to C#
                    this.codeDomProvider = CodeDomProvider.CreateProvider("C#");
                }
            }
            return this.codeDomProvider;
        }

        /// <summary>Gets the default extension of the output file from the CodeDomProvider</summary>
        /// <returns></returns>
        protected override string GetDefaultExtension()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            CodeDomProvider codeDom = this.GetCodeProvider();
            Debug.Assert(codeDom != null, "CodeDomProvider is NULL.");
            string extension = codeDom.FileExtension;
            if (extension != null && extension.Length > 0)
            {
                extension = ".generated." + extension.TrimStart(".".ToCharArray());
            }
            return extension;
        }

        /// <summary>Returns the <c>EnvDTE.<see cref="ProjectItem"/></c> object that corresponds to the project item the code generator was called on</summary>
        /// <returns>The <c>EnvDTE.<see cref="ProjectItem"/></c> of the project item the code generator was called on</returns>
        protected ProjectItem GetProjectItem()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            object p = this.GetService(typeof(ProjectItem));
            Debug.Assert(p != null, "Unable to get Project Item.");
            return (ProjectItem)p;
        }

        /// <summary>Returns the <c>EnvDTE.<see cref="Project"/></c> object of the project containing the project item the code generator was called on</summary>
        /// <returns>The <c>EnvDTE.<see cref="Project"/></c> object of the project containing the project item the code generator was called on</returns>
        protected Project GetProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return this.GetProjectItem().ContainingProject;
        }
    }
}
