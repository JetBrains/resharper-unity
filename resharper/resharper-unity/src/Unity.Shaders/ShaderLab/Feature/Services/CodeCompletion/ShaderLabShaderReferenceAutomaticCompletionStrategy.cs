﻿#nullable enable
using JetBrains.Application.Parts;
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
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class ShaderLabShaderReferenceAutomaticCompletionStrategy : IAutomaticCodeCompletionStrategy
    {
        private readonly ShaderLabIntellisenseManager myShaderLabIntellisenseManager;
        private readonly SettingsScalarEntry myScalarEntry;

        public ShaderLabShaderReferenceAutomaticCompletionStrategy(ShaderLabIntellisenseManager shaderLabIntellisenseManager, ISettingsStore settingsStore)
        {
            myShaderLabIntellisenseManager = shaderLabIntellisenseManager;
            myScalarEntry = settingsStore.Schema.GetScalarEntry((ShaderLabAutopopupEnabledSettingsKey key) => key.InShaderReferences);
        }

        public AutopopupType IsEnabledInSettings(IContextBoundSettingsStore settingsStore, ITextControl textControl) => (AutopopupType)settingsStore.GetValue(myScalarEntry, null);

        public bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore boundSettingsStore) => myShaderLabIntellisenseManager.GetAutopopupEnabled(boundSettingsStore);

        public bool ProcessSubsequentTyping(char c, ITextControl textControl) => c is not '"';

        public bool AcceptsFile(IFile file, ITextControl textControl) => file is IShaderLabFile && this.MatchToken(file, textControl, token => token.GetContainingNode<IShaderReferenceElement>() is not null);

        public PsiLanguageType Language => ShaderLabLanguage.Instance!;
        public bool ForceHideCompletion => false;
    }
}