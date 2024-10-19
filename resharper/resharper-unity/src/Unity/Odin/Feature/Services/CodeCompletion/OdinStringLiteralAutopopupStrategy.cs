using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Settings;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AutomaticStrategies;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeCompletion;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class OdinStringLiteralAutopopupStrategy : CSharpInStringLiteralAutopopupStrategy
{
    private readonly UnityTechnologyDescriptionCollector myTechnologyDescriptionCollector;

    public OdinStringLiteralAutopopupStrategy(UnityTechnologyDescriptionCollector technologyDescriptionCollector, [NotNull] CSharpCodeCompletionManager codeCompletionManager, [NotNull] ISettingsSchema settingsSchema) : base(codeCompletionManager, settingsSchema)
    {
        myTechnologyDescriptionCollector = technologyDescriptionCollector;
    }
    
    // TODO check for ODIN
    public override bool AcceptsFile(IFile file, ITextControl textControl)
    {
        if (!myTechnologyDescriptionCollector.DiscoveredTechnologies.ContainsKey("Odin"))
            return false;
        
        return base.AcceptsFile(file, textControl) && this.MatchToken(file, textControl, node =>
        {
            var expression = node.Parent as ICSharpLiteralExpression;
            var argument = CSharpArgumentNavigator.GetByValue(expression);
            var attribute = AttributeNavigator.GetByArgument(argument);
            return attribute != null;
        });
    }

    
    public override bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore settingsStore)
    {
        if (c == '$')
            return true;
        
        return base.AcceptTyping(c, textControl, settingsStore);
    }
}