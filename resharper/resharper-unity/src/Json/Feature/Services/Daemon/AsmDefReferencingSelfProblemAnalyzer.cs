using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Json.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Daemon
{
    [ElementProblemAnalyzer(typeof(IJavaScriptLiteralExpression))]
    public class AsmDefReferencingSelfProblemAnalyzer : AsmDefProblemAnalyzer<IJavaScriptLiteralExpression>
    {
        protected override void Analyze(IJavaScriptLiteralExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.IsReferencesStringLiteralValue())
            {
                var nameCache = data.Solution.GetComponent<AsmDefNameCache>();
                var nameDeclaredElement = nameCache.GetNameDeclaredElement(data.SourceFile);
                var reference = element.FindReference<AsmDefNameReference>();
                if (reference != null && nameDeclaredElement != null &&
                    Equals(reference.Resolve().DeclaredElement, nameDeclaredElement))
                {
                    consumer.AddHighlighting(new ReferencingSelfError(reference));
                }
            }
        }
    }
}