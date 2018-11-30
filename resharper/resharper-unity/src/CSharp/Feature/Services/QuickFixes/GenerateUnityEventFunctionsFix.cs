using JetBrains.ReSharper.Feature.Services.Generate;
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

        protected override void ConfigureContext(IGeneratorContext context)
        {
            // Do nothing. Calling base will add all values into InputElements, which means EVERYTHING is checked to be
            // implemented, which never makes sense for our list of event functions.
            // This base class is essentially for a Quick Fix that is to implement missing members, while we're really
            // using it like a Context Action
        }
    }
}