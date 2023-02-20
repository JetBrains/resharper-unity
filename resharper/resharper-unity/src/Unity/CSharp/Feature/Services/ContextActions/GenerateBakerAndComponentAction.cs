using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.UnityDots_GenerateBakerAndComponent_Name), 
        DescriptionResourceName = nameof(Strings.UnityDots_GenerateBakerAndComponent_Description))]
    public class GenerateBakerAndComponentAction : IContextAction
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public GenerateBakerAndComponentAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }
        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
            Assertion.Assert(node != null, "node != null");
            var classDeclaration = node.GetContainingNode<IClassLikeDeclaration>();

            var fix = new GenerateBakerAndComponentActionFix(classDeclaration, node);
            
            var action = new IntentionAction(fix, UnityGutterIcons.UnityLogo.Id, new SubmenuAnchor(BulbMenuAnchors.PermanentBackgroundItems, SubmenuBehavior.Executable));

            return new[] {action};
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
            
            var classDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
            if (classDeclaration == null) 
                return false;

            if (node.GetContainingNode<IMethodDeclaration>() != null ||
                node.GetContainingNode<IPropertyDeclaration>() != null)
                return false;
            
            return UnityApi.IsDerivesFromComponent(classDeclaration.DeclaredElement)
                && myDataProvider.Solution.HasEntitiesPackage();

        }
    }
}