#nullable enable

using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion
{
    [SolutionComponent]
    public class ShaderLabAutomaticStrategyOnKeyword : IAutomaticCodeCompletionStrategy
    {
        private readonly ShaderLabIntellisenseManager myShaderLabIntellisenseManager;
        private readonly SettingsScalarEntry myScalarEntry;

        public ShaderLabAutomaticStrategyOnKeyword(ShaderLabIntellisenseManager shaderLabIntellisenseManager, ISettingsStore settingsStore)
        {
            myShaderLabIntellisenseManager = shaderLabIntellisenseManager;
            myScalarEntry = settingsStore.Schema.GetScalarEntry((ShaderLabAutopopupEnabledSettingsKey key) => key.InKeywords);
        }
        
        public PsiLanguageType Language => ShaderLabLanguage.Instance!;
        public AutopopupType IsEnabledInSettings(IContextBoundSettingsStore settingsStore, ITextControl textControl) => (AutopopupType) settingsStore.GetValue(myScalarEntry, null);

        public bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore settingsStore)
        {
            if (!myShaderLabIntellisenseManager.GetAutopopupEnabled(settingsStore))
                return false;
            return IsPartOfKeyword(c);
        }

        public bool ProcessSubsequentTyping(char c, ITextControl textControl) => IsPartOfKeyword(c);

        public bool AcceptsFile(IFile file, ITextControl textControl) => file.Language.Is<ShaderLabLanguage>() && this.MatchToken(file, textControl, tt => tt.GetContainingNode<IVariableReference>() is null);

        public bool ForceHideCompletion => false;

        private bool IsPartOfKeyword(char c) => char.IsLetter(c);
    }
}
