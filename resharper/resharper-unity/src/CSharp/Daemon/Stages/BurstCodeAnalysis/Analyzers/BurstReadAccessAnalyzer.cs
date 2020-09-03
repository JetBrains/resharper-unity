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
    public class BurstReadAccessAnalyzer : BurstProblemAnalyzerBase<IReferenceExpression>
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

            if (!referenceExpression.GetAccessType().HasFlag(ExpressionAccessType.Read) || 
                !typeMember.IsStatic ||
                typeMember.IsReadonly ||
                typeMember.IsConstant() ||
                typeMember.IsEnumMember() ||
                typeMember is IProperty prop && !prop.IsWritable && prop.IsReadable) 
                return false;
            
            consumer?.AddHighlighting(new BurstLoadingStaticNotReadonlyWarning(referenceExpression.GetDocumentRange(),
                typeMember.GetContainingType()?.ShortName + "." + element.ShortName));
            
            return true;
        }
    }
}