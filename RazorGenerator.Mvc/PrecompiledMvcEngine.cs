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
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.WebPages;

namespace RazorGenerator.Mvc
{
    public class PrecompiledMvcEngine : VirtualPathProviderViewEngine, IVirtualPathFactory
    {
        private readonly IDictionary<string, Type> _mappings;
        private readonly IViewPageActivator _viewPageActivator;

        protected string BaseVirtualPath { get; private set; }
        protected Lazy<DateTime> AssemblyLastWriteTime { get; private set; }

        public PrecompiledMvcEngine(Assembly assembly)
            : this(assembly, null)
        {
        }

        public PrecompiledMvcEngine(Assembly assembly, string baseVirtualPath)
            : this(assembly, baseVirtualPath, null)
        {
        }

        public readonly static string[] DefaultAreaLocationFormats= new[] {
            "~/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/Areas/{2}/Views/{1}/{0}.vbhtml",
            "~/Areas/{2}/Views/Shared/{0}.cshtml",
            "~/Areas/{2}/Views/Shared/{0}.vbhtml",
        };

        public readonly static string[] DefaultLocationFormats = new[] {
            "~/Views/{1}/{0}.cshtml",
            "~/Views/{1}/{0}.vbhtml",
            "~/Views/Shared/{0}.cshtml",
            "~/Views/Shared/{0}.vbhtml",
        };

        public readonly static string[] DefaultFileExtensions = new[] {
            "cshtml",
            "vbhtml",
        };

        public PrecompiledMvcEngine(Assembly assembly, string baseVirtualPath, IViewPageActivator viewPageActivator)
        {
            base.AreaViewLocationFormats = DefaultAreaLocationFormats;
            base.AreaMasterLocationFormats = DefaultAreaLocationFormats;
            base.AreaPartialViewLocationFormats = DefaultAreaLocationFormats;
            base.ViewLocationFormats = DefaultLocationFormats;
            base.MasterLocationFormats = DefaultLocationFormats;
            base.PartialViewLocationFormats = DefaultLocationFormats;
            base.FileExtensions = DefaultFileExtensions;

            AssemblyLastWriteTime = new Lazy<DateTime>(() => assembly.GetLastWriteTimeUtc(fallback: DateTime.MaxValue));
            BaseVirtualPath = NormalizeBaseVirtualPath(baseVirtualPath);

            _mappings = (from type in assembly.GetTypes()
                         where typeof(WebPageRenderingBase).IsAssignableFrom(type)
                         let pageVirtualPath = type.GetCustomAttributes(inherit: false).OfType<PageVirtualPathAttribute>().FirstOrDefault()
                         where pageVirtualPath != null
                         select new KeyValuePair<string, Type>(CombineVirtualPaths(BaseVirtualPath, pageVirtualPath.VirtualPath), type)
                         ).ToDictionary(t => t.Key, t => t.Value, StringComparer.OrdinalIgnoreCase);
            this.ViewLocationCache = new PrecompiledViewLocationCache(assembly.FullName, this.ViewLocationCache);
            _viewPageActivator = viewPageActivator
                ?? DependencyResolver.Current.GetService<IViewPageActivator>() /* For compatibility, remove this line within next version */
                ?? DefaultViewPageActivator.Current;
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
            virtualPath = EnsureVirtualPathPrefix(virtualPath);

            if (UsePhysicalViewsIfNewer && IsPhysicalFileNewer(virtualPath))
            {
                // If the physical file on disk is newer and the user's opted in this behavior, serve it instead.
                return false;
            }
            return Exists(virtualPath);
        }

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            partialPath = EnsureVirtualPathPrefix(partialPath);

            return CreateViewInternal(partialPath, masterPath: null, runViewStartPages: false);
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            viewPath = EnsureVirtualPathPrefix(viewPath);

            return CreateViewInternal(viewPath, masterPath, runViewStartPages: true);
        }

        private IView CreateViewInternal(string viewPath, string masterPath, bool runViewStartPages)
        {
            Type type;
            if (_mappings.TryGetValue(viewPath, out type))
            {
                return new PrecompiledMvcView(viewPath, masterPath, type, runViewStartPages, base.FileExtensions, _viewPageActivator);
            }
            return null;
        }

        public object CreateInstance(string virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);
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
                return _viewPageActivator.Create((ControllerContext)null, type);
            }
            return null;
        }

        public bool Exists(string virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);

            return _mappings.ContainsKey(virtualPath);
        }

        protected virtual bool IsPhysicalFileNewer(string virtualPath)
        {
            return IsPhysicalFileNewer(virtualPath, BaseVirtualPath, AssemblyLastWriteTime);
        }

        internal static bool IsPhysicalFileNewer(string virtualPath, string baseVirtualPath, Lazy<DateTime> assemblyLastWriteTime)
        {
            if (virtualPath.StartsWith(baseVirtualPath ?? String.Empty, StringComparison.OrdinalIgnoreCase))
            {
                // If a base virtual path is specified, we should remove it as a prefix. Everything that follows should map to a view file on disk.
                if (!String.IsNullOrEmpty(baseVirtualPath))
                {
                    virtualPath = "~/" + virtualPath.Substring(baseVirtualPath.Length);
                }

                string path = HostingEnvironment.MapPath(virtualPath);
                return File.Exists(path) && File.GetLastWriteTimeUtc(path) > assemblyLastWriteTime.Value;
            }
            return false;
        }

        internal static string EnsureVirtualPathPrefix(string virtualPath)
        {
            if (!String.IsNullOrEmpty(virtualPath))
            {
                // For a virtual path lookups to succeed, it needs to start with a ~/.
                if (!virtualPath.StartsWith("~/", StringComparison.Ordinal))
                {
                    virtualPath = "~/" + virtualPath.TrimStart(new[] { '/', '~' });
                }
            }
            return virtualPath;
        }

        internal static string NormalizeBaseVirtualPath(string virtualPath)
        {
            if (!String.IsNullOrEmpty(virtualPath))
            {
                // For a virtual path to combine properly, it needs to start with a ~/ and end with a /.
                virtualPath = EnsureVirtualPathPrefix(virtualPath);
                if (!virtualPath.EndsWith("/", StringComparison.Ordinal))
                {
                    virtualPath += "/";
                }
            }
            return virtualPath;
        }

        private static string CombineVirtualPaths(string baseVirtualPath, string virtualPath)
        {
            if (!String.IsNullOrEmpty(baseVirtualPath))
            {
                return VirtualPathUtility.Combine(baseVirtualPath, virtualPath.Substring(2));
            }
            return virtualPath;
        }
    }
}
