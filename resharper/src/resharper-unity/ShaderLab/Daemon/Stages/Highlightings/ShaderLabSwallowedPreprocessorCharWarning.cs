using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.WARNING, "ShaderLabWarnings", Languages = ShaderLabLanguage.Name,
        AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE,
        OverlapResolve = OverlapResolveKind.DEADCODE, ToolTipFormatString = MESSAGE)]
    public class ShaderLabSwallowedPreprocessorCharWarning : IHighlighting, IUnityHighlighting
    {
        public ShaderLabSwallowedPreprocessorCharWarning(ITokenNode swallowedToken)
        {
            SwallowedToken = swallowedToken;
        }

        public const string MESSAGE = "Ignored character. Consider inserting new line for clarity";

        public ITokenNode SwallowedToken { get; }

        public bool IsValid() => SwallowedToken == null || SwallowedToken.IsValid();
        public DocumentRange CalculateRange() => SwallowedToken.GetHighlightingRange();
        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}