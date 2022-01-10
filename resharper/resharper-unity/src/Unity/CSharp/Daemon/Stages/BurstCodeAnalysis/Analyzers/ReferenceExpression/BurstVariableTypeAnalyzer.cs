using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent]
    public class BurstVariableTypeAnalyzer : IBurstProblemSubAnalyzer<IReferenceExpression>
    {
        private static bool HasAllowingBurstAttribute(ITypeOwner typeOwner)
        {
            var allowingAttribute = KnownTypes.NativeSetClassTypeToNullOnScheduleAttribute;

            return typeOwner is IAttributesOwner attributesOwner &&
                   attributesOwner.HasAttributeInstance(allowingAttribute, AttributesSource.Self);
        }

        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;

            if (element is not ITypeOwner typeOwner)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            if (HasAllowingBurstAttribute(typeOwner))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var type = typeOwner.Type();
            var innerType = (type as IArrayType)?.ElementType ?? type;

            if (BurstCodeAnalysisUtil.IsBurstPermittedType(innerType))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var typeElement = innerType?.GetTypeElement();
            var name = typeElement?.ShortName;

            if (!name.IsNullOrEmpty())
                consumer?.AddHighlighting(new BurstLoadingManagedTypeWarning(referenceExpression, name));

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 2000;
    }
}