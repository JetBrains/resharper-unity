using System;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Highlighting
{
    [ElementProblemAnalyzer(typeof(IMethodDeclaration), HighlightingTypes = new[] {typeof(UnityMarkOnGutter)})]
    public class UnityMessageDetector : ElementProblemAnalyzer<IMethodDeclaration>
    {
        private readonly UnityApi myUnityApi;

        public UnityMessageDetector(UnityApi unityApi)
        {
            myUnityApi = unityApi;
        }

        protected override void Run(IMethodDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (data.ProcessKind != DaemonProcessKind.VISIBLE_DOCUMENT)
                return;

            if (element.GetProject().IsUnityProject())
            {
                var method = element.DeclaredElement;
                if (method != null)
                {
                    var message = myUnityApi.GetUnityMessage(method);
                    if (message != null)
                    {
                        var documentRange = element.GetDocumentRange();
                        var tooltip = "Unity 3D Message";
                        if (!string.IsNullOrEmpty(message.Description))
                            tooltip += Environment.NewLine + Environment.NewLine + message.Description;
                        var highlighting = new UnityMarkOnGutter(element, documentRange, tooltip);

                        consumer.AddHighlighting(highlighting, documentRange);
                    }
                }
            }
        }
    }
}