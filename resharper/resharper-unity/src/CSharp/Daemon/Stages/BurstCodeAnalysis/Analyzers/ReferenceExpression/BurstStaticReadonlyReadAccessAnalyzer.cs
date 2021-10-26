using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent]
    public class BurstStaticReadonlyReadAccessAnalyzer : IBurstProblemSubAnalyzer<IReferenceExpression>
    {
        // all static fields in burst must be readonly
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;

            //non auto property are not interested cuz they are not prohibited,
            //and any backing field will be handled inside accessor 
            if (element is not IProperty {IsAuto: true, IsStatic: true} && element is not IField {IsStatic: true})
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var typeMember = (ITypeMember) element;
            var accessType = referenceExpression.GetAccessType();

            if (!accessType.HasFlag(ExpressionAccessType.Read))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            switch (typeMember)
            {
                case IField when typeMember.IsConstant():
                case IField when typeMember.IsEnumMember():
                case IProperty {IsWritable: false, IsReadable: true}:
                case IField {IsReadonly: true}:
                    return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
            }

            var containingTypeElement = typeMember.GetContainingType();
            var containingTypeName = containingTypeElement?.ShortName;
            var name = element.ShortName;

            if (!name.IsNullOrEmpty())
            {
                if (!containingTypeName.IsNullOrEmpty())
                    name = containingTypeName + "." + name;

                consumer?.AddHighlighting(new BurstLoadingStaticNotReadonlyWarning(referenceExpression, name));
            }

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 3000;
    }
}