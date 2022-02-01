using JetBrains.RdBackend.Common.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi;
using JetBrains.ReSharper.Daemon.Syntax;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.Cg.Daemon.SyntaxHighlighting
{
    [Language(typeof(CgLanguage))]
    public class CgSyntaxHighlightingManager : SyntaxHighlightingManager
    {
        public override SyntaxHighlightingProcessor CreateProcessor()
        {
            return new CgSyntaxHighlightingProcessor();
        }
    }
}