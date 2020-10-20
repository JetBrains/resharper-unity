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
    public class BurstReadAccessAnalyzer : IBurstProblemSubAnalyzer<IReferenceExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;
            
            //non auto property are not interested cuz they are not prohibited,
            //and any backing field will be handled inside accessor 
            if ((!(element is IProperty property) || !property.IsAuto) && !(element is IField))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var typeMember = (ITypeMember) element;

            if (!referenceExpression.GetAccessType().HasFlag(ExpressionAccessType.Read) || 
                !typeMember.IsStatic ||
                typeMember.IsReadonly ||
                typeMember.IsConstant() ||
                typeMember.IsEnumMember() ||
                typeMember is IProperty prop && !prop.IsWritable && prop.IsReadable) 
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
            
            consumer?.AddHighlighting(new BurstLoadingStaticNotReadonlyWarning(referenceExpression,
                typeMember.GetContainingType()?.ShortName + "." + element.ShortName));
            
            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 3000;
    }
}