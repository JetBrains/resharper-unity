using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.WARNING, "ShaderLabWarnings", Languages = ShaderLabLanguage.Name,
        AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE,
        OverlapResolve = OverlapResolveKind.DEADCODE, ToolTipFormatString = MESSAGE)]
    public class ShaderLabSwallowedPreprocessorCharWarning : IHighlighting, IUnityHighlighting
    {
        private readonly ITokenNode mySwallowedToken;

        public ShaderLabSwallowedPreprocessorCharWarning(ITokenNode swallowedToken)
        {
            mySwallowedToken = swallowedToken;
        }

        public const string MESSAGE = "Ignored character. Consider replacing with new line";

        public bool IsValid() => mySwallowedToken == null || mySwallowedToken.IsValid();
        public DocumentRange CalculateRange() => mySwallowedToken.GetHighlightingRange();
        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}