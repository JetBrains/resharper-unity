using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstVariableTypeAnalyzer: BurstProblemAnalyzerBase<IReferenceExpression>
    {
        protected override bool CheckAndAnalyze(IReferenceExpression referenceExpression, IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;

            if (!(element is ITypeOwner typeOwner)) 
                return false;

            if (typeOwner is IAttributesOwner attributesOwner &&
                attributesOwner.HasAttributeInstance(KnownTypes.NativeSetClassTypeToNullOnScheduleAttribute, AttributesSource.Self))
                return false;

            if (BurstCodeAnalysisUtil.IsBurstPermittedType(typeOwner.Type())) 
                return false;

            consumer?.AddHighlighting(new BurstLoadingManagedTypeWarning(
                referenceExpression.GetDocumentRange(),
                typeOwner.Type().GetTypeElement()?.ShortName));

            return true;

        }
    }
}