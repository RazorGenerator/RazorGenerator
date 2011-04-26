using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using System.Web.WebPages;

namespace Microsoft.Web.PrecompiledMvcView {
    public class PrecompiledMvcEngine : VirtualPathProviderViewEngine, IVirtualPathFactory {
        private readonly IDictionary<string, Type> _mappings;
        public PrecompiledMvcEngine(Assembly assembly) {
            base.AreaViewLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/{1}/{0}.vbhtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.vbhtml"
            };

            base.AreaMasterLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/{1}/{0}.vbhtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.vbhtml"
            };

            base.AreaPartialViewLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/{1}/{0}.vbhtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.vbhtml"
            };
            base.ViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/{1}/{0}.vbhtml", 
                "~/Views/Shared/{0}.cshtml", 
                "~/Views/Shared/{0}.vbhtml"
            };
            base.MasterLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/{1}/{0}.vbhtml", 
                "~/Views/Shared/{0}.cshtml", 
                "~/Views/Shared/{0}.vbhtml"
            };
            base.PartialViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/{1}/{0}.vbhtml", 
                "~/Views/Shared/{0}.cshtml", 
                "~/Views/Shared/{0}.vbhtml"
            };
            base.FileExtensions = new[] {
                "cshtml", 
                "vbhtml"
            };

            _mappings = (from type in assembly.GetExportedTypes()
                         where typeof(WebPageRenderingBase).IsAssignableFrom(type)
                         let pageVirtualPath = type.GetCustomAttributes(inherit: false).OfType<PageVirtualPathAttribute>().FirstOrDefault()
                         where pageVirtualPath != null
                         select new KeyValuePair<string, Type>(pageVirtualPath.VirtualPath, type)
                         ).ToDictionary(t => t.Key, t => t.Value, StringComparer.OrdinalIgnoreCase);
        }

        protected override bool FileExists(ControllerContext controllerContext, string virtualPath) {
            return Exists(virtualPath);
        }

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath) {
            bool runViewStartPages = false;
            Type type;
            if (_mappings.TryGetValue(partialPath, out type)) {
                return new PrecompiledMvcView(partialPath, type, runViewStartPages, base.FileExtensions);
            }
            return null;
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath) {
            bool runViewStartPages = true;
            Type type;
            if (_mappings.TryGetValue(viewPath, out type)) {
                return new PrecompiledMvcView(viewPath, type, runViewStartPages, base.FileExtensions);
            }
            return null;
        }

        public object CreateInstance(string virtualPath) {
            Type type;
            if (_mappings.TryGetValue(virtualPath, out type)) {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public bool Exists(string virtualPath) {
            return _mappings.ContainsKey(virtualPath);
        }
    }
}
