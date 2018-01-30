using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IConditionalAccessExpression), HighlightingTypes = new[] { typeof(UnityNullPropagationWarning) })]
    public class UnityNullPropagationProblemAnalyzer : UnityElementProblemAnalyzer<IConditionalAccessExpression>
    {
        public UnityNullPropagationProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IConditionalAccessExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (expression.ConditionalQualifier == null || expression.ConditionalAccessSign == null)
                return;
            if (!expression.HasConditionalAccessSign)
                return;
            if (!(expression.ConditionalQualifier is IReferenceExpression qualifier) || !IsDescendantOfUnityObject(qualifier))
                return;

            consumer.AddHighlighting(new UnityNullPropagationWarning(expression));
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
