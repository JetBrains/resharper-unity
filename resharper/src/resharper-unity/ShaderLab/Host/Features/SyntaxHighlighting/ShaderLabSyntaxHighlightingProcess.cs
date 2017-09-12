#if RIDER

using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Host.Features.SyntaxHighlighting
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
            return tokenType == ShaderLabTokenType.NUMERIC_LITERAL || tokenType == ShaderLabTokenType.PP_DIGITS;
        }

        protected override bool IsKeyword(TokenNodeType tokenType)
        {
            return tokenType == ShaderLabTokenType.PP_ERROR
                   || tokenType == ShaderLabTokenType.PP_WARNING
                   || tokenType == ShaderLabTokenType.PP_LINE
                   || tokenType == ShaderLabTokenType.CG_INCLUDE
                   || tokenType == ShaderLabTokenType.GLSL_INCLUDE
                   || tokenType == ShaderLabTokenType.HLSL_INCLUDE;
        }
    }
}

#endif