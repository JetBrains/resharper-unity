using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.ERROR, "ShaderLabErrors", Languages = "SHADERLAB", OverlapResolve = OverlapResolveKind.ERROR, ToolTipFormatString = MESSAGE)]
    public class ShaderLabErrorPreprocessorDirectiveError : IHighlighting, IUnityHighlighting
    {
        private const string MESSAGE = "{0}";

        private readonly IPpErrorDirective myDirectiveNode;

        public ShaderLabErrorPreprocessorDirectiveError(IPpErrorDirective directiveNode, string message)
        {
            myDirectiveNode = directiveNode;
            ToolTip = string.Format(MESSAGE, message);
        }

        public bool IsValid() => myDirectiveNode == null || myDirectiveNode.IsValid();
        public DocumentRange CalculateRange() => myDirectiveNode.Directive.GetHighlightingRange();
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}