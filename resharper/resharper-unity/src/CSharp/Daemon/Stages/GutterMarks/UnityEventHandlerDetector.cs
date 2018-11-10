using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IMethodDeclaration), typeof(IPropertyDeclaration), HighlightingTypes = new[] {typeof(UnityGutterMarkInfo)})]
    public class UnityEventHandlerDetector : UnityElementProblemAnalyzer<IDeclaration>
    {
        private readonly UnityEventHandlerReferenceCache myCache;

        public UnityEventHandlerDetector([NotNull] UnityApi unityApi, UnityEventHandlerReferenceCache cache)
            : base(unityApi)
        {
            myCache = cache;
        }

        protected override void Analyze(IDeclaration element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            var declaredElement = element.DeclaredElement;
            if (declaredElement != null && myCache.IsEventHandler(declaredElement))
            {
                var highlighting = new UnityGutterMarkInfo(element, "Unity event handler");
                consumer.AddHighlighting(highlighting);
            }
        }
    }
}