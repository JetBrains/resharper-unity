using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

[ContextAction(GroupType = typeof(CSharpUnityContextActions),
    ResourceType = typeof(Strings), NameResourceName = nameof(Strings.UnityDots_GenerateComponentReference_Name), 
    DescriptionResourceName = nameof(Strings.UnityDots_GenerateComponentReference_Description))]
public class GenerateComponentReferencesAction : IContextAction
{
    private readonly ICSharpContextActionDataProvider myDataProvider;

    public GenerateComponentReferencesAction(ICSharpContextActionDataProvider dataProvider)
    {
        myDataProvider = dataProvider;
    }
    public IEnumerable<IntentionAction> CreateBulbItems()
    {
        var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
        Assertion.Assert(node != null, "node != null");
        var classLikeDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
            
        var fix = new GenerateComponentReferencesActionFix(classLikeDeclaration, node);
            
        var action = new IntentionAction(fix, PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id, 
            new SubmenuAnchor(IntentionsAnchors.HighPriorityContextActionsAnchor, SubmenuBehavior.Executable));

        return new[] {action};
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
        var classDeclaration = myDataProvider.GetSelectedTreeNode<IClassLikeDeclaration>();
        return classDeclaration != null && 
               classDeclaration.DeclaredElement.DerivesFrom(KnownTypes.IAspect);
    }
}