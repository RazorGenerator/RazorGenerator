/** 
 * This is based on PrecompiledViews written by Chris van de Steeg. 
 * Code and discussions for Chris's changes are available at (http://www.chrisvandesteeg.nl/2010/11/22/embedding-pre-compiled-razor-views-in-your-dll/)
 **/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.WebPages;

namespace RazorGenerator.Mvc
{
    public class PrecompiledMvcEngine : VirtualPathProviderViewEngine, IVirtualPathFactory
    {
        private readonly IDictionary<string, Type> _mappings;
        private readonly DateTime _assemblyCreatedTime;
        public PrecompiledMvcEngine(Assembly assembly)
        {
            _assemblyCreatedTime = File.GetLastWriteTimeUtc(assembly.Location);

            base.AreaViewLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
            };

            base.AreaMasterLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
            };

            base.AreaPartialViewLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
            };
            base.ViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/Shared/{0}.cshtml", 
            };
            base.MasterLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/Shared/{0}.cshtml", 
            };
            base.PartialViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/Shared/{0}.cshtml", 
            };
            base.FileExtensions = new[] {
                "cshtml", 
            };

            _mappings = (from type in assembly.GetTypes()
                         where typeof(WebPageRenderingBase).IsAssignableFrom(type)
                         let pageVirtualPath = type.GetCustomAttributes(inherit: false).OfType<PageVirtualPathAttribute>().FirstOrDefault()
                         where pageVirtualPath != null
                         select new KeyValuePair<string, Type>(pageVirtualPath.VirtualPath, type)
                         ).ToDictionary(t => t.Key, t => t.Value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if IVirtualPathFactory lookups returns files from assembly regardless of whether physical files are available for the virtual path.
        /// </summary>
        public bool PreemptPhysicalFiles
        {
            get; set;
        }

        /// <summary>
        /// Determines if the view engine uses views on disk if it's been changed since the view assembly was last compiled.
        /// </summary>
        /// <remarks>
        /// What an awful name!
        /// </remarks>
        public bool UsePhysicalViewsIfNewer
        {
            get; set;
        }

        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            if (UsePhysicalViewsIfNewer && IsPhysicalFileNewer(virtualPath))
            {
                // If the physical file on disk is newer and the user's opted in this behavior, serve it instead.
                return false;
            }
            return Exists(virtualPath);
        }

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            return CreateViewInternal(partialPath, runViewStartPages: false);
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            return CreateViewInternal(viewPath, runViewStartPages: true);
        }

        private IView CreateViewInternal(string viewPath, bool runViewStartPages)
        {
            Type type;
            if (_mappings.TryGetValue(viewPath, out type))
            {
                return new PrecompiledMvcView(viewPath, type, runViewStartPages, base.FileExtensions);
            }
            return null;
        }

        public object CreateInstance(string virtualPath)
        {
            Type type;

            if (!PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath))
            {
                // If we aren't pre-empting physical files, use the BuildManager to create _ViewStart instances if the file exists on disk. 
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebPageRenderingBase));
            }

            if (UsePhysicalViewsIfNewer && IsPhysicalFileNewer(virtualPath))
            {
                // If the physical file on disk is newer and the user's opted in this behavior, serve it instead.
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebViewPage));
            }

            if (_mappings.TryGetValue(virtualPath, out type))
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public bool Exists(string virtualPath)
        {
            return _mappings.ContainsKey(virtualPath);
        }

        private bool IsPhysicalFileNewer(string virtualPath)
        {
            string path = HttpContext.Current.Request.MapPath(virtualPath);
            return File.Exists(path) && File.GetLastWriteTimeUtc(path) > _assemblyCreatedTime;
        }
    }
}
