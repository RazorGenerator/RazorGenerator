/** 
 * This is based on PrecompiledViews written by Chris van de Steeg. 
 * Code and discussions for Chris's changes are available at (http://www.chrisvandesteeg.nl/2010/11/22/embedding-pre-compiled-razor-views-in-your-dll/)
 **/
using System;
using System.Collections.Generic;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.WebPages;

namespace RazorGenerator.Mvc
{
    public class CompositePrecompiledMvcEngine : VirtualPathProviderViewEngine, IVirtualPathFactory
    {
        private readonly IDictionary<string, ViewMapping> _mappings 
            = new Dictionary<string, ViewMapping>(StringComparer.OrdinalIgnoreCase);
        private readonly IViewPageActivator _viewPageActivator;

        public CompositePrecompiledMvcEngine(params PrecompiledViewAssembly[] viewAssemblies)
            : this(viewAssemblies, null)
        {
        }

        public CompositePrecompiledMvcEngine(IEnumerable<PrecompiledViewAssembly> viewAssemblies, IViewPageActivator viewPageActivator)
        {
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

            foreach (var viewAssembly in viewAssemblies)
            {
                foreach (var mapping in viewAssembly.GetTypeMappings())
                {
                    _mappings[mapping.Key] = new ViewMapping { Type = mapping.Value, ViewAssembly = viewAssembly };
                }
            }
            
            _viewPageActivator = viewPageActivator
                ?? DependencyResolver.Current.GetService<IViewPageActivator>() /* For compatibility, remove this line within next version */
                ?? DefaultViewPageActivator.Current;
        }

        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            virtualPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(virtualPath);

            ViewMapping mapping;
            if (!_mappings.TryGetValue(virtualPath, out mapping))
            {
                return false;
            }

            if (mapping.ViewAssembly.UsePhysicalViewsIfNewer && mapping.ViewAssembly.IsPhysicalFileNewer(virtualPath))
            {
                // If the physical file on disk is newer and the user's opted in this behavior, serve it instead.
                return false;
            }
            return Exists(virtualPath);
        }

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            partialPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(partialPath);

            return CreateViewInternal(partialPath, masterPath: null, runViewStartPages: false);
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            viewPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(viewPath);

            return CreateViewInternal(viewPath, masterPath, runViewStartPages: true);
        }

        private IView CreateViewInternal(string viewPath, string masterPath, bool runViewStartPages)
        {
            ViewMapping mapping;
            if (_mappings.TryGetValue(viewPath, out mapping))
            {
                return new PrecompiledMvcView(viewPath, masterPath, mapping.Type, runViewStartPages, base.FileExtensions, _viewPageActivator);
            }
            return null;
        }

        public object CreateInstance(string virtualPath)
        {
            virtualPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(virtualPath);

            ViewMapping mapping;

            if (!_mappings.TryGetValue(virtualPath, out mapping))
            {
                return null;
            }

            if (!mapping.ViewAssembly.PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath))
            {
                // If we aren't pre-empting physical files, use the BuildManager to create _ViewStart instances if the file exists on disk. 
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebPageRenderingBase));
            }

            if (mapping.ViewAssembly.UsePhysicalViewsIfNewer && mapping.ViewAssembly.IsPhysicalFileNewer(virtualPath))
            {
                // If the physical file on disk is newer and the user's opted in this behavior, serve it instead.
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebViewPage));
            }

            return _viewPageActivator.Create((ControllerContext)null, mapping.Type);
        }

        public bool Exists(string virtualPath)
        {
            virtualPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(virtualPath);

            return _mappings.ContainsKey(virtualPath);
        }

        private struct ViewMapping
        {
            public Type Type { get; set; }
            public PrecompiledViewAssembly ViewAssembly { get; set; }
        }
    }
}
