using System.Web;
using System.Web.Mvc;

namespace RazorGenerator.Mvc
{
    /// <remarks>
    /// There's an issue in Mvc's View location's caching behavior. The cache key is derived from the assembly's type name
    /// consequently it expects exactly one instance of a type to be registered. We workaround this by overriding the behavior of 
    /// the ViewLocationCache to include a token that discriminates multiple view engine instances. 
    /// </remarks>
    public class PrecompiledViewLocationCache : IViewLocationCache
    {
        private readonly string _assemblyName;
        private readonly IViewLocationCache _innerCache;

        public PrecompiledViewLocationCache(string assemblyName, IViewLocationCache innerCache)
        {
            _assemblyName = assemblyName;
            _innerCache = innerCache;
        }

        public string GetViewLocation(HttpContextBase httpContext, string key)
        {
            key = _assemblyName + "::" + key;
            return _innerCache.GetViewLocation(httpContext, key);
        }

        public void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath)
        {
            key = _assemblyName + "::" + key;
            _innerCache.InsertViewLocation(httpContext, key, virtualPath);
        }
    }
}
