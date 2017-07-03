#if RIDER

using JetBrains.ReSharper.Host.Features.Foldings;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Host.Features.Foldings.ShaderLab
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

#endif