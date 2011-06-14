using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using RazorGenerator;
using RazorGenerator.RazorHost;

namespace HostExtensions {
    [Export("MyAwesomeHost", typeof(ISingleFileGenerator))]
    public class TestHost : BasicHost {
        
        public override void  PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod) {
 	        base.PostProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            var getter = new CodeMemberProperty() { 
                Name = "CustomProperty",
                Type = new CodeTypeReference(typeof(String))
            };
            getter.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression("Hello world")));

            generatedClass.Members.Add(getter);
        }
    }
}
