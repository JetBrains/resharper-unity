using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IConditionalAccessExpression), HighlightingTypes = new[] { typeof(UnityNullConditionalWarning) })]
    public class UnityNullConditionalProblemAnalyzer : UnityElementProblemAnalyzer<IConditionalAccessExpression>
    {
        public UnityNullConditionalProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IConditionalAccessExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (expression.ConditionalQualifier == null || expression.ConditionalAccessSign == null)
                return;
            if (!expression.HasConditionalAccessSign)
                return;

            if (expression.ConditionalQualifier is IReferenceExpression qualifier && IsDescendantOfUnityObject(qualifier))
            {
                consumer.AddHighlighting(new UnityNullConditionalWarning(expression));
            }
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
