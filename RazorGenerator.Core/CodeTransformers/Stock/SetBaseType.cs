using System;
using System.Collections.Generic;

namespace RazorGenerator.Core.CodeTransformers
{
    public class SetBaseType : RazorCodeTransformerBase
    {
        private readonly string _typeName;
        private readonly bool _override;

        public SetBaseType(string typeName, bool @override = false)
        {
            this._typeName = typeName;
            this._override = @override;
        }

        public SetBaseType(Type type, bool @override = false)
            : this(type.FullName, @override: @override)
        {
        }

        private bool IsDefaultBaseClass(string baseClass)
        {
            const String System_Web_WebPages_WebPage = "System.Web.WebPages.WebPage";

            return string.IsNullOrEmpty(baseClass) || System_Web_WebPages_WebPage == baseClass;
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            if (this._override || this.IsDefaultBaseClass(razorHost.DefaultBaseClass))
            {
                razorHost.DefaultBaseClass = this._typeName;
            }
        }
    }
}
