using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[] {typeof(UnityGutterMarkInfo)})]
    public class UnityTypeDetector : UnityElementProblemAnalyzer<IClassLikeDeclaration>
    {
        public UnityTypeDetector(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IClassLikeDeclaration element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var @class = element.DeclaredElement;
            if (@class != null && Api.IsUnityType(@class))
            {
                var highlighting = new UnityGutterMarkInfo(element, "Unity scripting component");
                consumer.AddHighlighting(highlighting);
            }
        }
    }
}