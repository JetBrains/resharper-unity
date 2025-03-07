#nullable enable

using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeCompletion;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class OdinStringLiteralAutopopupStrategy : IAutomaticCodeCompletionStrategy
{
    private readonly UnityTechnologyDescriptionCollector myTechnologyDescriptionCollector;
    private readonly CSharpCodeCompletionManager myCodeCompletionManager;
    private readonly SettingsScalarEntry mySettingsEntry;

    public OdinStringLiteralAutopopupStrategy(UnityTechnologyDescriptionCollector technologyDescriptionCollector, CSharpCodeCompletionManager codeCompletionManager, ISettingsSchema settingsSchema)
    {
        myTechnologyDescriptionCollector = technologyDescriptionCollector;
        myCodeCompletionManager = codeCompletionManager;
        mySettingsEntry = settingsSchema.GetScalarEntry((CSharpAutopopupEnabledSettingsKey key) => key.InStringLiterals);
    }

    public PsiLanguageType Language => CSharpLanguage.Instance.NotNull();

    public AutopopupType IsEnabledInSettings(IContextBoundSettingsStore settingsStore, ITextControl textControl)
    {
        return (AutopopupType)settingsStore.GetValue(mySettingsEntry, null);
    }

    // TODO check for ODIN
    public bool AcceptsFile(IFile file, ITextControl textControl)
    {
        if (!myTechnologyDescriptionCollector.DiscoveredTechnologies.ContainsKey("Odin"))
            return false;
        
        return this.MatchTokenType(file, textControl, type => type.IsStringLiteral) && this.MatchToken(file, textControl, node =>
        {
            var expression = node.Parent as ICSharpLiteralExpression;
            var argument = CSharpArgumentNavigator.GetByValue(expression);
            var attribute = AttributeNavigator.GetByArgument(argument);
            return attribute != null;
        });
    }

    public bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore settingsStore)
    {
        if (!myCodeCompletionManager.GetAutopopupEnabled(settingsStore))
            return false;

        return c == '$';
    }

    public bool ProcessSubsequentTyping(char c, ITextControl textControl) => char.IsLetterOrDigit(c);

    public bool ForceHideCompletion => false;
}