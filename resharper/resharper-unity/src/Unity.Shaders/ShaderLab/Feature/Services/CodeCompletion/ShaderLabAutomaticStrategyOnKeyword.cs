#nullable enable

using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion
{
    [SolutionComponent]
    public class ShaderLabAutomaticStrategyOnKeyword : IAutomaticCodeCompletionStrategy
    {
        private readonly ISolution mySolution;
        private readonly ShaderLabIntellisenseManager myShaderLabIntellisenseManager;
        private readonly SettingsScalarEntry myScalarEntry;

        public ShaderLabAutomaticStrategyOnKeyword(ISolution solution, ShaderLabIntellisenseManager shaderLabIntellisenseManager, ISettingsStore settingsStore)
        {
            myShaderLabIntellisenseManager = shaderLabIntellisenseManager;
            mySolution = solution;
            myScalarEntry = settingsStore.Schema.GetScalarEntry((ShaderLabAutopopupEnabledSettingsKey key) => key.InKeywords);
        }
        
        public PsiLanguageType Language => ShaderLabLanguage.Instance!;
        public AutopopupType IsEnabledInSettings(IContextBoundSettingsStore settingsStore, ITextControl textControl) => (AutopopupType) settingsStore.GetValue(myScalarEntry, null);

        public bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore settingsStore)
        {
            if (!myShaderLabIntellisenseManager.GetAutopopupEnabled(settingsStore))
                return false;
            if (IsPartOfKeyword(c))
                return true;
            if (textControl.Document.GetPsiSourceFile(mySolution) is not { } sourceFile)
                return false;
            var documentOffset = textControl.Caret.DocumentOffset();
            if (sourceFile.GetPsiFile<ShaderLabLanguage>(documentOffset) is not { } psiFile)
                return false;
            return psiFile.FindNodeAt(documentOffset) is { } node && IsKeywordExpected(node);
        }

        public bool ProcessSubsequentTyping(char c, ITextControl textControl) => IsPartOfKeyword(c);

        public bool AcceptsFile(IFile file, ITextControl textControl) => file.Language.Is<ShaderLabLanguage>();

        public bool ForceHideCompletion => false;

        private bool IsPartOfKeyword(char c) => char.IsLetter(c);

        private bool IsKeywordExpected(ITreeNode node)
        {
            var mayBeUnexpectedToken = node.NodeType switch
            {
                ITokenNodeType { IsWhitespace: true } => node.FindPreviousNode(_ => TreeNodeActionType.ACCEPT),
                IFixedTokenNodeType => node.FindNextNode(_ => TreeNodeActionType.ACCEPT),
                _ => null
            };

            return mayBeUnexpectedToken is UnexpectedTokenErrorElement errorElement
                   && errorElement.ExpectedTokenTypes.Any(x => x is ITokenNodeType { IsKeyword: true });
        }
    }
}
