using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.ShaderLab.Daemon.CodeFolding
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