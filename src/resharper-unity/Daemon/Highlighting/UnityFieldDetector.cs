using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Highlighting
{
    [ElementProblemAnalyzer(typeof(IFieldDeclaration), HighlightingTypes = new[] { typeof(UnityMarkOnGutter) })]
    public class UnityFieldDetector : ElementProblemAnalyzer<IFieldDeclaration>
    {
        private readonly UnityApi myUnityApi;

        public UnityFieldDetector(UnityApi unityApi)
        {
            myUnityApi = unityApi;
        }

        protected override void Run(IFieldDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (data.ProcessKind != DaemonProcessKind.VISIBLE_DOCUMENT)
                return;

            if (element.GetProject().IsUnityProject())
            {
                var field = element.DeclaredElement;
                if (field != null && myUnityApi.IsUnityField(field))
                {
                    var documentRange = element.GetDocumentRange();
                    var highlighting = new UnityMarkOnGutter(element, documentRange, "Unity 3D Field");

                    consumer.AddHighlighting(highlighting, documentRange);
                }
            }
        }
    }
}