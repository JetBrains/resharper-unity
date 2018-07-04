using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    public class GenerateUnityEventFunctionsFix :
        WorkflowDrivenImplementMembersFix<GenerateUnityEventFunctionsWorkflow>
    {
        public GenerateUnityEventFunctionsFix(IClassLikeDeclaration typeDeclaration)
            : base(typeDeclaration)
        {
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return true;
        }

        public override string Text => "Generate Unity event functions";
    }
}