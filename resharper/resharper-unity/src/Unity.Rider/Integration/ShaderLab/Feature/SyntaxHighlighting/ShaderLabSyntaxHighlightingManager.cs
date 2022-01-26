using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.RdBackend.Common.Env;
using JetBrains.RdBackend.Common.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.ShaderLab.Feature.SyntaxHighlighting
{
    [Language(typeof (ShaderLabLanguage))]
    internal class ShaderLabSyntaxHighlightingManager : RiderSyntaxHighlightingManager
    {
        public override SyntaxHighlightingProcessor CreateProcessor()
        {
            return new ShaderLabSyntaxHighlightingProcessor();
        }
    }
}