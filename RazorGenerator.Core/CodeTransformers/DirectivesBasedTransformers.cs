using System;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
    public class DirectivesBasedTransformers : AggregateCodeTransformer
    {
        public static readonly string TypeVisibilityKey = "TypeVisibility";
        public static readonly string DisableLinePragmasKey = "DisableLinePragmas";
        public static readonly string TrimLeadingUnderscoresKey = "TrimLeadingUnderscores";
        public static readonly string GenerateAbsolutePathLinePragmas = "GenerateAbsolutePathLinePragmas";
        private readonly List<IRazorCodeTransformer> _transformers = new List<IRazorCodeTransformer>();

        protected override IEnumerable<IRazorCodeTransformer> CodeTransformers
        {
            get { return _transformers; }
        }

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            if (directives.ContainsKey(TypeVisibilityKey))
            {
                _transformers.Add(new SetTypeVisibility(directives[TypeVisibilityKey]));
            }

            if (ReadSwitchValue(directives, DisableLinePragmasKey) == true)
            {
                razorHost.EnableLinePragmas = false;
            }
            else if (ReadSwitchValue(directives, GenerateAbsolutePathLinePragmas) != true)
            {
                // Rewrite line pragamas to generate bin relative paths instead of absolute paths.
                _transformers.Add(new RewriteLinePragmas());
            }

            if (ReadSwitchValue(directives, TrimLeadingUnderscoresKey) != false)
            {
                // This should in theory be a different transformer.
                razorHost.DefaultClassName = razorHost.DefaultClassName.TrimStart('_');
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
