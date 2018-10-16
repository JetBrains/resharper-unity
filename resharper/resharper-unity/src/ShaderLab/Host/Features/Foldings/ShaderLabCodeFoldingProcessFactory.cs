using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Host.Features.Foldings
{
    [Language(typeof(ShaderLabLanguage))]
    internal class ShaderLabCodeFoldingProcessFactory : ICodeFoldingProcessorFactory
    {
        public ICodeFoldingProcessor CreateProcessor()
        {
            return new ShaderLabCodeFoldingProcessor();
        }
    }
}