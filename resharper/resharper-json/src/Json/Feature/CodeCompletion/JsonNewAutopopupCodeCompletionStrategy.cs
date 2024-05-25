using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Json.Feature.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Json.Feature.CodeCompletion
{
    [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class JsonNewAutopopupCodeCompletionStrategy : IAutomaticCodeCompletionStrategy
    {
        private readonly JsonNewIntellisenseManager myJsonIntellisenseManager;

        public JsonNewAutopopupCodeCompletionStrategy(JsonNewIntellisenseManager jsonNewIntellisenseManager)
        {
            myJsonIntellisenseManager = jsonNewIntellisenseManager;
        }

        public bool AcceptsFile(IFile file, ITextControl textControl)
        {
            return this.MatchTokenType(file, textControl, token => token.IsIdentifier || token.IsKeyword || token == JsonNewTokenNodeTypes.DOUBLE_QUOTED_STRING);
        }

        public bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore boundSettingsStore)
        {
            if (!myJsonIntellisenseManager.GetAutopopupEnabled(boundSettingsStore))
                return false;

            return true;
        }

        public bool ForceHideCompletion => false;

        // ReSharper disable once AssignNullToNotNullAttribute
        public PsiLanguageType Language => JsonNewLanguage.Instance;

        public AutopopupType IsEnabledInSettings(IContextBoundSettingsStore settingsStore, ITextControl textControl)
        {
            return AutopopupType.HardAutopopup;
        }

        public bool ProcessSubsequentTyping(char c, ITextControl textControl) => true;
    }
}