using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IFieldDeclaration), HighlightingTypes = new[] { typeof(UnityGutterMarkInfo) })]
    public class UnityFieldDetector : UnityElementProblemAnalyzer<IFieldDeclaration>
    {
        public UnityFieldDetector(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IFieldDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var field = element.DeclaredElement;
            if (field != null && Api.IsSerialisedField(field))
            {
                var highlighting = new UnityGutterMarkInfo(element, "This field is initialised by Unity");
                consumer.AddHighlighting(highlighting);
            }
        }
    }
}