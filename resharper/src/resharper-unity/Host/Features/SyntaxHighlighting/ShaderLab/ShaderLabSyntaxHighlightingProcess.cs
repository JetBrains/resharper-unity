#if RIDER

using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Host.Features.SyntaxHighlighting.ShaderLab
{
    internal class ShaderLabSyntaxHighlightingProcess : RiderSyntaxHighlightingProcessBase
    {
        public ShaderLabSyntaxHighlightingProcess([NotNull] IDaemonProcess process,
            [NotNull] IContextBoundSettingsStore settingsStore, [NotNull] IFile file)
            : base(process, settingsStore, file)
        {
        }

        protected override string GetAttributeId(TokenNodeType tokenType)
        {
            if (tokenType == ShaderLabTokenType.CG_CONTENT)
                return HighlightingAttributeIds.INJECT_STRING_BACKGROUND;

            return base.GetAttributeId(tokenType);
        }

        protected override bool IsBlockComment(TokenNodeType tokenType)
        {
            return tokenType == ShaderLabTokenType.MULTI_LINE_COMMENT;
        }

        protected override bool IsNumber(TokenNodeType tokenType)
        {
            return tokenType == ShaderLabTokenType.NUMERIC_LITERAL;
        }
    }
}

#endif