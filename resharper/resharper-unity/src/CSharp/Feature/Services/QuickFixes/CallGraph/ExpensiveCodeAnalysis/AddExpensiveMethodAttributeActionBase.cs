using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    public abstract class AddExpensiveMethodAttributeActionBase : CallGraphActionBase
    {
        protected const string MESSAGE = "Add Expensive method attribute";
        protected override IClrTypeName ProtagonistAttribute => KnownTypes.PublicApiAttribute;
        public override string Text => MESSAGE;

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var declaredElement = MethodDeclaration?.DeclaredElement;

            return declaredElement != null && MethodDeclaration.IsValid() &&
                   !declaredElement.HasAttributeInstance(ProtagonistAttribute, AttributesSource.Self);
        }
    }
}