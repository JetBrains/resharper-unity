#nullable enable
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Editor;
using JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.SelectEmbracingConstruct
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabSelectEmbracingConstructProvider : ISelectEmbracingConstructProvider
    {
        private readonly ISettingsStore mySettingsStore;

        public ShaderLabSelectEmbracingConstructProvider(ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore;
        }

        public bool IsAvailable(IPsiSourceFile sourceFile) => sourceFile.Properties.ShouldBuildPsi;

        public ISelectedRange? GetSelectedRange(IPsiSourceFile sourceFile, DocumentRange documentRange) => sourceFile.GetPsiFile<ShaderLabLanguage>(documentRange) is ShaderLabFile shaderLabFile ? GetSelectedRange(shaderLabFile, documentRange) : null;

        private ISelectedRange? GetSelectedRange(ShaderLabFile shaderLabFile, DocumentRange documentRange)
        {
            var translatedRange = shaderLabFile.Translate(documentRange);
            if (!translatedRange.IsValid())
                return null;
            return new ShaderLabDotSelection(shaderLabFile, translatedRange.StartOffset, selectBetterToken: translatedRange.Length == 0, useCamelHumps: EditorOptions.DoUseCamelHumps(mySettingsStore, shaderLabFile), appendInjectedPsi: true);
        }
    }
}