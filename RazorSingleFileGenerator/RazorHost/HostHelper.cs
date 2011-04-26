using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Microsoft.Web.RazorSingleFileGenerator.RazorHost {
    public static class HostHelper {

        public static string GetVirtualPath(string projectRelativePath) {
            return VirtualPathUtility.ToAppRelative("~" + projectRelativePath);
        }

        public static string GetClassName(string virtualPath) {
            virtualPath = virtualPath.TrimStart('~', '/', '_');
            return Regex.Replace(virtualPath, @"[\/.]", "_");
        }
    }
}
