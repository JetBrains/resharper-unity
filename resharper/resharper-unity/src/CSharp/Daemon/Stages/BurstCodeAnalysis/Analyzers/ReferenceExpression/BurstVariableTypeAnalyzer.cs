using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent]
    public class BurstVariableTypeAnalyzer: IBurstProblemSubAnalyzer<IReferenceExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IReferenceExpression referenceExpression, IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;

            if (!(element is ITypeOwner typeOwner)) 
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            if (typeOwner is IAttributesOwner attributesOwner &&
                attributesOwner.HasAttributeInstance(KnownTypes.NativeSetClassTypeToNullOnScheduleAttribute, AttributesSource.Self))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            if (BurstCodeAnalysisUtil.IsBurstPermittedType(typeOwner.Type())) 
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            consumer?.AddHighlighting(new BurstLoadingManagedTypeWarning(
                referenceExpression,
                typeOwner.Type().GetTypeElement()?.ShortName));

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;

        }

        public int Priority => 2000;
    }
}