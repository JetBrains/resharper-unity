using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [SolutionComponent]
    public class ShaderLabVariableReferenceAutomaticCompletionStrategy : IAutomaticCodeCompletionStrategy
    {
        private readonly ShaderLabIntellisenseManager myShaderLabIntellisenseManager;
        private readonly SettingsScalarEntry myScalarEntry;

        public ShaderLabVariableReferenceAutomaticCompletionStrategy(ShaderLabIntellisenseManager shaderLabIntellisenseManager, ISettingsStore settingsStore)
        {
            myShaderLabIntellisenseManager = shaderLabIntellisenseManager;
            myScalarEntry =
                settingsStore.Schema.GetScalarEntry((ShaderLabAutopopupEnabledSettingsKey key) =>
                    key.InVariableReferences);
        }

        public AutopopupType IsEnabledInSettings(IContextBoundSettingsStore settingsStore, ITextControl textControl)
        {
            return (AutopopupType) settingsStore.GetValue(myScalarEntry, null);
        }

        public bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore boundSettingsStore)
        {
            if (!myShaderLabIntellisenseManager.GetAutoppopupEnabled(boundSettingsStore))
                return false;
            return c == '[';
        }

        public bool ProcessSubsequentTyping(char c, ITextControl textControl)
        {
            // What does this mean?
            return true;
        }

        public bool AcceptsFile(IFile file, ITextControl textControl)
        {
            return file is IShaderLabFile;
        }

        public PsiLanguageType Language => ShaderLabLanguage.Instance;
        public bool ForceHideCompletion => false;
    }
}