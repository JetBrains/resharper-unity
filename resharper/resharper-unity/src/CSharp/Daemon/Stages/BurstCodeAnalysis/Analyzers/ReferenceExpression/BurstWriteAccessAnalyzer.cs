using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.ReferenceExpression
{
    [SolutionComponent]
    public class BurstWriteAccessAnalyzer : IBurstProblemSubAnalyzer<IReferenceExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;

            // non auto property are not interested cuz they are not prohibited,
            // and any backing field will be handled inside accessor 
            if (element is not (IProperty {IsAuto: true} or IField))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var typeMember = (ITypeMember) element;
            var accessType = referenceExpression.GetAccessType();

            if (accessType.HasFlag(ExpressionAccessType.Write) && typeMember.IsStatic)
            {
                //there are no static write-only auto properties
                var name = element.ShortName;

                if (!name.IsNullOrEmpty())
                    consumer?.AddHighlighting(new BurstWriteStaticFieldWarning(referenceExpression, name));

                return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
            }

            return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
        }

        public int Priority => 4000;
    }
}