using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    // This is to handle the "." in a fully qualified type name in e.g. GetComponent("UnityEngine.Grid")
    [SolutionComponent]
    public class UnityObjectTypeReferenceAutomaticCompletionStrategy : IAutomaticCodeCompletionStrategy
    {
        public AutopopupType IsEnabledInSettings(IContextBoundSettingsStore settingsStore, ITextControl textControl)
        {
            return AutopopupType.HardAutopopup;
        }

        public bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore boundSettingsStore)
        {
            return c == '.';
        }

        public bool ProcessSubsequentTyping(char c, ITextControl textControl)
        {
            // Don't know what this means...
            return true;
        }

        public bool AcceptsFile(IFile file, ITextControl textControl)
        {
            return file is ICSharpFile && file.IsFromUnityProject() &&
                   this.MatchTokenType(file, textControl, type => type.IsStringLiteral);
        }

        public PsiLanguageType Language => CSharpLanguage.Instance;
        public bool ForceHideCompletion => false;
    }
}