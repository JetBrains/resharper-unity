using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class BurstVirtualPropertyAnalyzer : IBurstProblemSubAnalyzer<IReferenceExpression>
    {
        private static bool IsVirtual([NotNull] IProperty property)
        {
            return property.IsVirtual || property.IsOverride || property.IsAbstract;
        }

        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(
            IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;

            if (element is not IProperty property)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            if (!IsVirtual(property))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var typeElement = property.ContainingType;

            if (typeElement is IInterface && referenceExpression.GetExtensionQualifier()?.Type().IsValueType() == true)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var typeElementShortName = typeElement?.ShortName;
            var name = property.ShortName;

            if (!name.IsNullOrEmpty())
            {
                if (!typeElementShortName.IsNullOrEmpty())
                    name = typeElementShortName + "." + name;

                consumer?.AddHighlighting(new BurstLoadingManagedTypeWarning(referenceExpression, name));
            }

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 1000;
    }
}