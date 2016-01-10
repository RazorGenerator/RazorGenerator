using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.WebPages;

namespace RazorGenerator.Mvc
{
    public class PrecompiledViewAssembly
    {
        private readonly string _baseVirtualPath;
        private readonly Assembly _assembly;
        private readonly Lazy<DateTime> _assemblyLastWriteTime;

        public PrecompiledViewAssembly(Assembly assembly)
            : this(assembly, null)
        {
        }

        public PrecompiledViewAssembly(Assembly assembly, string baseVirtualPath)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            _baseVirtualPath = PrecompiledMvcEngine.NormalizeBaseVirtualPath(baseVirtualPath);
            _assembly = assembly;
            _assemblyLastWriteTime = new Lazy<DateTime>(() => _assembly.GetLastWriteTimeUtc(fallback: DateTime.MaxValue));
        }

        public static PrecompiledViewAssembly OfType<T>(
            string baseVirtualPath, bool usePhysicalViewsIfNewer = false, bool preemptPhysicalFiles = false)
        {
            return new PrecompiledViewAssembly(typeof(T).Assembly, baseVirtualPath)
            {
                UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
                PreemptPhysicalFiles = preemptPhysicalFiles
            };
        }

        public static PrecompiledViewAssembly OfType<T>(bool usePhysicalViewsIfNewer = false, bool preemptPhysicalFiles = false)
        {
            return new PrecompiledViewAssembly(typeof(T).Assembly)
                {
                    UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
                    PreemptPhysicalFiles = preemptPhysicalFiles
                };
        }

        /// <summary>
        /// Determines if IVirtualPathFactory lookups returns files from assembly regardless of whether physical files are available for the virtual path.
        /// </summary>
        public bool PreemptPhysicalFiles { get; set; }

        /// <summary>
        /// Determines if the view engine uses views on disk if it's been changed since the view assembly was last compiled.
        /// </summary>
        /// <remarks>
        /// What an awful name!
        /// </remarks>
        public bool UsePhysicalViewsIfNewer { get; set; }

        public IDictionary<string, Type> GetTypeMappings()
        {
            return (from type in _assembly.GetTypes()
                    where typeof(WebPageRenderingBase).IsAssignableFrom(type)
                    let pageVirtualPath = type.GetCustomAttributes(inherit: false).OfType<PageVirtualPathAttribute>().FirstOrDefault()
                    where pageVirtualPath != null
                    select new KeyValuePair<string, Type>(CombineVirtualPaths(_baseVirtualPath, pageVirtualPath.VirtualPath), type)
                   ).ToDictionary(t => t.Key, t => t.Value, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsPhysicalFileNewer(string virtualPath)
        {
            return PrecompiledMvcEngine.IsPhysicalFileNewer(virtualPath, _baseVirtualPath, _assemblyLastWriteTime);
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