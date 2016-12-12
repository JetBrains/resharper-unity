using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Highlighting
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[] {typeof(UnityMarkOnGutter)})]
    public class UnityTypeDetector : ElementProblemAnalyzer<IClassLikeDeclaration>
    {
        private readonly UnityApi myUnityApi;

        public UnityTypeDetector(UnityApi unityApi)
        {
            myUnityApi = unityApi;
        }

        protected override void Run(IClassLikeDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (data.ProcessKind != DaemonProcessKind.VISIBLE_DOCUMENT)
                return;

            if (element.GetProject().IsUnityProject())
            {
                var @class = element.DeclaredElement;
                if (@class != null)
                {
                    if (myUnityApi.IsUnityType(@class))
                    {
                        var documentRange = element.GetNameDocumentRange();
                        var highlighting = new UnityMarkOnGutter(element, documentRange, "Unity Scripting Type");

                        consumer.AddHighlighting(highlighting, documentRange);
                    }
                }
            }
        }
    }
}