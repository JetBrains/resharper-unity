using JetBrains.Annotations;
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
    public class BurstManagedTypeAnalyzer : IBurstProblemSubAnalyzer<IReferenceExpression>
    {
        private static bool IsManaged([NotNull] IModifiersOwner modifiersOwner)
        {
            return modifiersOwner.IsVirtual || modifiersOwner.IsOverride || modifiersOwner.IsAbstract;
        }

        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;
            var typeOwner = element as ITypeOwner;

            if (typeOwner == null)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var modifiersOwner = element as IModifiersOwner;

            if (modifiersOwner == null)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            if (!IsManaged(modifiersOwner))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            //virtual and abstract cannot be in struct. only override is getHashCode -> function
            consumer?.AddHighlighting(new BurstLoadingManagedTypeWarning(referenceExpression.GetDocumentRange(),
                typeOwner.Type().GetTypeElement()?.ShortName + "." + element.ShortName));

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 1000;
    }
}