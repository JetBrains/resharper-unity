using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstWriteAccessAnalyzer : BurstProblemAnalyzerBase<IReferenceExpression>
    {
        protected override bool CheckAndAnalyze(IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;

            //non auto property are not interested cuz they are not prohibited,
            //and any backing field will be handled inside accessor 
            if ((!(element is IProperty property) || !property.IsAuto) && !(element is IField))
                return false;

            var typeMember = (ITypeMember) element;

            if (!referenceExpression.GetAccessType().HasFlag(ExpressionAccessType.Write) ||
                !typeMember.IsStatic)
                return false;

            //there are no static write-only auto properties
            consumer?.AddHighlighting(new BurstWriteStaticFieldWarning(referenceExpression.GetDocumentRange(),
                element.ShortName));

            return true;
        }
    }
}