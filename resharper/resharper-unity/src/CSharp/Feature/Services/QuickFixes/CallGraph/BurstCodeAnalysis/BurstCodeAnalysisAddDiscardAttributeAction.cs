using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public abstract class BurstCodeAnalysisAddDiscardAttributeAction : CallGraphActionBase
    {
        protected const string Message = "Add BurstDiscard attribute";
        protected override IClrTypeName ProtagonistAttribute => KnownTypes.BurstDiscardAttribute;
        protected override IClrTypeName AntagonistAttribute => null;
        public override string Text => Message;
        public sealed override bool IsAvailable(IUserDataHolder cache)
        {
            var declaredElement = MethodDeclaration?.DeclaredElement;

            return MethodDeclaration != null && MethodDeclaration.IsValid() &&
                   declaredElement != null && !BurstCodeAnalysisUtil.IsBurstContextBannedFunction(declaredElement) &&
                   !declaredElement.HasAttributeInstance(ProtagonistAttribute, AttributesSource.Self);
        }
    }
}