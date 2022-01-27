using JetBrains.ReSharper.Daemon.Syntax;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.ShaderLab.Feature.SyntaxHighlighting
{
    [Language(typeof (ShaderLabLanguage))]
    internal class ShaderLabSyntaxHighlightingManager : SyntaxHighlightingManager
    {
        public override SyntaxHighlightingProcessor CreateProcessor()
        {
            return new ShaderLabSyntaxHighlightingProcessor();
        }
    }
}