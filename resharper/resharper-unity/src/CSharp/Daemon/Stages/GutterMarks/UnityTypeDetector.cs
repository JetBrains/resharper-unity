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
            var typeElement = element.DeclaredElement;
            if (typeElement != null)
            {
                if (Api.IsUnityType(typeElement))
                {
                    var highlighting = new UnityGutterMarkInfo(element, "Unity scripting component");
                    consumer.AddHighlighting(highlighting);
                }
                else if (Api.IsSerializableType(typeElement))
                {
                    var highlighting = new UnityGutterMarkInfo(element, "Unity custom serializable type");
                    consumer.AddHighlighting(highlighting);
                }
            }
        }
    }
}