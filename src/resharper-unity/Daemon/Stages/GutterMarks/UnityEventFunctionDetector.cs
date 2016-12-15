using System;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IMethodDeclaration), HighlightingTypes = new[] {typeof(UnityMarkOnGutter)})]
    public class UnityEventFunctionDetector : UnityElementProblemAnalyzer<IMethodDeclaration>
    {
        public UnityEventFunctionDetector(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IMethodDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var method = element.DeclaredElement;
            if (method != null)
            {
                var eventFunction = Api.GetUnityEventFunction(method);
                if (eventFunction != null)
                {
                    // Use the name as the range, rather than the range of the whole
                    // method declaration (including body). Rider will remove the highlight
                    // if anything inside the range changes, causing ugly flashes. It
                    // might be nicer to use the whole of the method declaration (name + params)
                    var documentRange = element.GetNameDocumentRange();
                    var tooltip = "Unity event function";
                    if (!string.IsNullOrEmpty(eventFunction.Description))
                        tooltip += Environment.NewLine + Environment.NewLine + eventFunction.Description;
                    AddGutterMark(element, documentRange, tooltip, consumer);
                }
            }
        }
    }
}