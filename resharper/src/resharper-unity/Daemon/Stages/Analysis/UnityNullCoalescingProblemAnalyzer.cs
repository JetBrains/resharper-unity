using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
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
            if (expression.LeftOperand == null || expression.RightOperand == null)
                return;
            if (!(expression.LeftOperand is IReferenceExpression leftOperand) || !IsDescendantOfUnityObject(leftOperand))
                return;

            consumer.AddHighlighting(new UnityNullCoalescingWarning(expression));
        }

        private static bool IsDescendantOfUnityObject([CanBeNull]IReferenceExpression expression)
        {
            var resolve = expression?.Reference.Resolve();
            if (resolve == null || resolve.ResolveErrorType != ResolveErrorType.OK)
                return false;

            var typeElement = expression.Type().GetTypeElement();
            if (typeElement == null)
                return false;

            return typeElement.IsDescendantOf(KnownTypes.Object);
        }
    }
}
