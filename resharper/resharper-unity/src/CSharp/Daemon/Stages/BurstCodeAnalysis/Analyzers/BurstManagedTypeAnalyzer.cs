using JetBrains.Annotations;
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
    public class BurstManagedTypeAnalyzer : BurstProblemAnalyzerBase<IReferenceExpression>
    {
        private static bool IsManaged([NotNull] IModifiersOwner modifiersOwner)
        {
            return modifiersOwner.IsVirtual || modifiersOwner.IsOverride || modifiersOwner.IsAbstract;
        }

        protected override bool CheckAndAnalyze(IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;
            var typeOwner = element as ITypeOwner;

            if (typeOwner == null)
                return false;

            var modifiersOwner = element as IModifiersOwner;

            if (modifiersOwner == null)
                return false;

            if (!IsManaged(modifiersOwner))
                return false;

            //virtual and abstract cannot be in struct. only override is getHashCode -> function
            consumer?.AddHighlighting(new BurstLoadingManagedTypeWarning(referenceExpression.GetDocumentRange(),
                typeOwner.Type().GetTypeElement()?.ShortName + "." + element.ShortName));

            return true;
        }
    }
}