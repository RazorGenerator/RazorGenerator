using System.Collections.Generic;
using System.Web.Razor;

namespace RazorGenerator.Core {
    public class DirectivesBasedTransformers : AggregateCodeTransformer {
        public static readonly string MakeTypeInternalKey = "MakeTypeInternal";
        public static readonly string DisableLinePragmasKey = "DisableLinePragmas";
        private readonly List<IRazorCodeTransformer> _transformers = new List<IRazorCodeTransformer>();

        protected override IEnumerable<IRazorCodeTransformer> CodeTransformers {
            get { return _transformers; }
        }

        public override void Initialize(RazorHost razorHost, string projectRelativePath, IDictionary<string, string> directives) {
            if (directives.ContainsKey(MakeTypeInternalKey)) {
                _transformers.Add(new MakeTypeInternal());
            }

            if (directives.ContainsKey(DisableLinePragmasKey)) {
                razorHost.EnableLinePragmas = false;
            }

            base.Initialize(razorHost, projectRelativePath, directives);
        }
    }
}
