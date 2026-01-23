using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.ShaderLab.Daemon.CodeFolding
{
    [Language(typeof(ShaderLabLanguage))]
    internal class ShaderLabCodeFoldingProcessFactory : ICodeFoldingProcessorFactory
    {
        public ICodeFoldingProcessor CreateProcessor(IContextBoundSettingsStore settingsStore)
        {
            return new ShaderLabCodeFoldingProcessor();
        }
    }
}