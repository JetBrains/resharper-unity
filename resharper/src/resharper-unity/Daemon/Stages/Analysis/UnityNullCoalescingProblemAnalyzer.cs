using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(INullCoalescingExpression), HighlightingTypes = new[] { typeof(UnityNullCoalescingWarning) })]
    public class UnityNullCoalescingProblemAnalyzer : UnityElementProblemAnalyzer<INullCoalescingExpression>
    {
        public UnityNullCoalescingProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(INullCoalescingExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!(expression.LeftOperand is IReferenceExpression leftOperand) || expression.RightOperand == null)
                return;
            var resolve = leftOperand.Reference.Resolve();
            if (resolve.ResolveErrorType != ResolveErrorType.OK)
                return;
            var unityObjectType = TypeFactory.CreateTypeByCLRName(KnownTypes.Object, expression.GetPsiModule());
            if (!leftOperand.Type().IsSubtypeOf(unityObjectType))
                return;

            consumer.AddHighlighting(new UnityNullCoalescingWarning(expression));
        }
    }
}
