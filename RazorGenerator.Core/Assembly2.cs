using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RazorGenerator.Core
{
    public static class Assembly2
    {
        /// <remarks>
        /// Attempts to locate where the RazorGenerator.Core assembly is being loaded from. This allows us to locate the v1 and v2 assemblies and the corresponding System.Web.* binaries
        /// Assembly.CodeBase points to the original location when the file is shadow copied, so we'll attempt to use that first.
        /// </remarks>
        public static DirectoryInfo GetExecutingAssemblyDirectory()
        {
            FileInfo executingAssemblyFile = GetExecutingAssemblyFilePathOrNull();
            if (executingAssemblyFile != null) return executingAssemblyFile.Directory;

            return null;
        }

        private static FileInfo GetExecutingAssemblyFilePathOrNull()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            try
            {
                {
                    FileInfo fromCodeBase = new FileInfo(assembly.CodeBase);
                    if (fromCodeBase.Exists) return fromCodeBase;
                }

                {
                    FileInfo fromLocation = new FileInfo(assembly.Location);
                    if (fromLocation.Exists) return fromLocation;
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
