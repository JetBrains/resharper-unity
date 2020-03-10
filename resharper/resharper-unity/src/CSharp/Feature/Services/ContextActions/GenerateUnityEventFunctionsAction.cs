using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Generate Unity event functions",
        Description = "Generate Unity event functions inside Unity type")]
    public class GenerateUnityEventFunctionsAction : IContextAction
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public GenerateUnityEventFunctionsAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
            Assertion.Assert(node != null, "node != null");
            var classDeclaration = node.GetContainingNode<IClassLikeDeclaration>();

            var fix = new GenerateUnityEventFunctionsFix(classDeclaration, node);

            //RIDER-30526
            var action = new IntentionAction(fix, PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id,
                new SubmenuAnchor(BulbMenuAnchors.PermanentBackgroundItems, SubmenuBehavior.Executable));

            return new[] {action};
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
            if (node == null)
                return false;

            var classDeclaration = node.GetContainingNode<IClassLikeDeclaration>();
            if (node.GetContainingNode<IMethodDeclaration>() == null && classDeclaration != null)
            {
                var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
                return unityApi.IsUnityType(classDeclaration.DeclaredElement);
            }

            return false;
        }
    }
}