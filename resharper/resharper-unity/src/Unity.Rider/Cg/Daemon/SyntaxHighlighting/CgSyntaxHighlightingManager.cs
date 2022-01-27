using JetBrains.ReSharper.Daemon.Syntax;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Cg.Daemon.SyntaxHighlighting
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