using JetBrains.Metadata.Reader.API;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    public abstract class ExpensiveCodeAnalysisAddExpensiveMethodAttributeActionBase : CallGraphActionBase
    {
        protected const string MESSAGE = "Add Expensive method attribute";
        protected override IClrTypeName ProtagonistAttribute => CallGraphActionUtil.ExpensiveMethodAttribute;

        protected override IClrTypeName AntagonistAttribute => null;

        public override string Text => MESSAGE;

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var declaredElement = MethodDeclaration?.DeclaredElement;

            return MethodDeclaration != null && MethodDeclaration.IsValid() &&
                   declaredElement != null;
        }
    }
}