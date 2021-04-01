using System;
using System.Globalization;

namespace RazorGenerator.Core
{
    public static class FormattingExtensions
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

        //

        /// <summary>Calls <see cref="IFormattable.ToString(string, IFormatProvider)"/> using <see cref="CultureInfo.InvariantCulture"/>. Does not box <paramref name="formattable"/>.</summary>
        public static string FmtInv<TFormattable>(this TFormattable formattable, string format)
            where TFormattable : IFormattable
        {
            if( formattable == null ) return null;

            return formattable.ToString( format: format, formatProvider: CultureInfo.InvariantCulture );
        }

        /// <summary>Calls <see cref="IFormattable.ToString(string, IFormatProvider)"/> using <see cref="CultureInfo.CurrentCulture"/>. Does not box <paramref name="formattable"/>.</summary>
        public static string Fmt<TFormattable>(this TFormattable formattable, string format)
            where TFormattable : IFormattable
        {
            if( formattable == null ) return null;

            return formattable.ToString( format: format, formatProvider: CultureInfo.CurrentCulture );
        }

        /// <summary>Calls <see cref="IConvertible.ToString(IFormatProvider)"/> using <see cref="CultureInfo.InvariantCulture"/>. Does not box <paramref name="formattable"/>.</summary>
        public static string FmtInv<TConvertible>(this TConvertible convertible)
            where TConvertible : IConvertible
        {
            if( convertible == null ) return null;

            return convertible.ToString( CultureInfo.InvariantCulture );
        }

        /// <summary>Calls <see cref="IConvertible.ToString(IFormatProvider)"/> using <see cref="CultureInfo.CurrentCulture"/>. Does not box <paramref name="formattable"/>.</summary>
        public static string Fmt<TConvertible>(this TConvertible convertible)
            where TConvertible : IConvertible
        {
            if( convertible == null ) return null;

            return convertible.ToString( CultureInfo.CurrentCulture );
        }
    }
}
