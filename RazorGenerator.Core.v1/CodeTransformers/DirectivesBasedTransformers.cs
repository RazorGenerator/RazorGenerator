using System;
using System.Collections.Generic;
using System.Linq;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    public class DirectivesBasedTransformers : AggregateCodeTransformer
    {
        public static readonly string TypeVisibilityKey               = "TypeVisibility";
        public static readonly string DisableLinePragmasKey           = "DisableLinePragmas";
        public static readonly string TrimLeadingUnderscoresKey       = "TrimLeadingUnderscores";
        public static readonly string GenerateAbsolutePathLinePragmas = "GenerateAbsolutePathLinePragmas";
        public static readonly string NamespaceKey                    = "Namespace";
        public static readonly string ExcludeFromCodeCoverage         = "ExcludeFromCodeCoverage";
        public static readonly string SuffixFileName                  = "ClassSuffix";
        public static readonly string GenericParametersKey            = "GenericParameters";
        public static readonly string ImportsKey                      = "Imports";
        public static readonly string BaseType                        = "BaseType";

        private readonly List<RazorCodeTransformerBase> _transformers = new List<RazorCodeTransformerBase>();

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return this._transformers; }
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            if (ReadSwitchValue(directives, GeneratePrettyNamesTransformer.DirectiveName) == true)
            {
                bool trimLeadingUnderscores = ReadSwitchValue(directives, TrimLeadingUnderscoresKey) ?? false;
                this._transformers.Add(new GeneratePrettyNamesTransformer(trimLeadingUnderscores));
            }

            string typeVisibility;
            if (directives.TryGetValue(TypeVisibilityKey, out typeVisibility))
            {
                this._transformers.Add(new SetTypeVisibility(typeVisibility));
            }

            string typeNamespace;
            if (directives.TryGetValue(NamespaceKey, out typeNamespace))
            {
                this._transformers.Add(new SetTypeNamespace(typeNamespace));
            }

            if (ReadSwitchValue(directives, DisableLinePragmasKey) == true)
            {
                razorHost.EnableLinePragmas = false;
            }
            else if (ReadSwitchValue(directives, GenerateAbsolutePathLinePragmas) != true)
            {
                // Rewrite line pragamas to generate bin relative paths instead of absolute paths.
                this._transformers.Add(new RewriteLinePragmas());
            }

            if (ReadSwitchValue(directives, ExcludeFromCodeCoverage) == true)
            {
                this._transformers.Add(new ExcludeFromCodeCoverageTransformer());
            }

            string suffix;
            if (directives.TryGetValue(SuffixFileName, out suffix))
            {
                this._transformers.Add(new SuffixFileNameTransformer(suffix));
            }

            string genericParameters;
            if (directives.TryGetValue(GenericParametersKey, out genericParameters))
            {
                IEnumerable<string> parameters = from p in genericParameters.Split(',') select p.Trim();
                this._transformers.Add(new GenericParametersTransformer(parameters));
            }

            string imports;
            if (directives.TryGetValue(ImportsKey, out imports))
            {
                IEnumerable<string> values = from p in imports.Split(',') select p.Trim();
                this._transformers.Add(new SetImports(values, false));
            }

            string baseType;
            if (directives.TryGetValue(BaseType, out baseType))
            {
                this._transformers.Add(new SetBaseType(baseType, @override: true));
            }

            base.Initialize(razorHost, directives);
        }

        private static bool? ReadSwitchValue(IDictionary<string, string> directives, string key)
        {
            string value;
            bool switchValue;

            if (directives.TryGetValue(key, out value) && Boolean.TryParse(value, out switchValue))
            {
                return switchValue;
            }
            return null;
        }
    }
}
