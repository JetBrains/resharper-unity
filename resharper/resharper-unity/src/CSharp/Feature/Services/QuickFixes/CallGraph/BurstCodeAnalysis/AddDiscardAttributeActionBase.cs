using JetBrains.Collections;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public abstract class AddDiscardAttributeActionBase : CallGraphActionBase
    {
        protected const string Message = "Add BurstDiscard attribute";
        protected override IClrTypeName ProtagonistAttribute => KnownTypes.BurstDiscardAttribute;
        protected override CompactList<AttributeValue> FixedArguments => new CompactList<AttributeValue>();
        public override string Text => Message;

        public sealed override bool IsAvailable(IUserDataHolder cache)
        {
            var declaredElement = MethodDeclaration?.DeclaredElement;

            return declaredElement != null && MethodDeclaration.IsValid() &&
                   !BurstCodeAnalysisUtil.IsBurstProhibitedFunction(declaredElement) &&
                   !declaredElement.HasAttributeInstance(ProtagonistAttribute, AttributesSource.Self);
        }
    }
}