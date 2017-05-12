using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[] {typeof(UnityMarkOnGutter)})]
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
                AddGutterMark(element, element.GetNameDocumentRange(), "Unity scripting component", consumer);
        }
    }
}