using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorGenerator
{
    internal static class Extensions
    {
        /// <summary>Formats <paramref name="format"/> using <see cref="CultureInfo.CurrentCulture"/>.</summary>
        /// <param name="format">Required. Cannot be <see langword="null"/>.</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Fmt(this string format, params object[] args)
        {
            return String.Format(provider: CultureInfo.CurrentCulture, format: format, args: args);
        }

        /// <summary>Formats <paramref name="format"/> using <see cref="CultureInfo.InvariantCulture"/>.</summary>
        /// <param name="format">Required. Cannot be <see langword="null"/>.</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string FmtInv(this string format, params object[] args)
        {
            return String.Format(provider: CultureInfo.InvariantCulture, format: format, args: args);
        }
    }
}
