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
    [ElementProblemAnalyzer(typeof(IConditionalAccessExpression), HighlightingTypes = new[] { typeof(UnityNullPropagationWarning) })]
    public class UnityNullPropagationProblemAnalyzer : UnityElementProblemAnalyzer<IConditionalAccessExpression>
    {
        public UnityNullPropagationProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IConditionalAccessExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!expression.HasConditionalAccessSign)
                return;
            if (!(expression.ConditionalQualifier is IReferenceExpression qualifier))
                return;
            var resolve = qualifier.Reference.Resolve();
            if (resolve.ResolveErrorType != ResolveErrorType.OK)
                return;
            var unityObjectType = TypeFactory.CreateTypeByCLRName(KnownTypes.Object, expression.GetPsiModule());
            if (!qualifier.Type().IsSubtypeOf(unityObjectType))
                return;

            consumer.AddHighlighting(new UnityNullPropagationWarning(expression));
        }
    }
}
