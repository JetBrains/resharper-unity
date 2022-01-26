using JetBrains.RdBackend.Common.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Cg.Daemon.SyntaxHighlighting
{
    [Language(typeof(CgLanguage))]
    public class CgSyntaxHighlightingManager : RiderSyntaxHighlightingManager
    {
        public override SyntaxHighlightingProcessor CreateProcessor()
        {
            return new CgSyntaxHighlightingProcessor();
        }
    }
}