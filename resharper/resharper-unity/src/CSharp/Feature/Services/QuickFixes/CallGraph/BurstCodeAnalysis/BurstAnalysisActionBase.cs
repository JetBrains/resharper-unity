using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public abstract class BurstAnalysisActionBase : CallGraphActionBase
    {
        protected BurstAnalysisActionBase(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected BurstAnalysisActionBase(IBurstHighlighting burstHighlighting) : base(burstHighlighting)
        {
        }

        protected BurstAnalysisActionBase(IMethodDeclaration methodDeclaration) : base(methodDeclaration)
        {
        }
        
        public sealed override bool IsAvailable(IUserDataHolder cache)
        {
            var declaredElement = MethodDeclaration?.DeclaredElement;

            return MethodDeclaration != null && MethodDeclaration.IsValid() &&
                   declaredElement != null && !BurstCodeAnalysisUtil.IsBurstContextBannedFunction(declaredElement) &&
                   !declaredElement.HasAttributeInstance(ProtagonistAttribute, AttributesSource.Self);
        }
    }
}