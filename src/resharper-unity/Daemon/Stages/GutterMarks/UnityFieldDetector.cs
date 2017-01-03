using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IFieldDeclaration), HighlightingTypes = new[] { typeof(UnityMarkOnGutter) })]
    public class UnityFieldDetector : UnityElementProblemAnalyzer<IFieldDeclaration>
    {
        public UnityFieldDetector(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IFieldDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var field = element.DeclaredElement;
            if (field != null && Api.IsUnityField(field))
                AddGutterMark(element, element.GetNameDocumentRange(), "This field is initialised by Unity", consumer);
        }
    }
}