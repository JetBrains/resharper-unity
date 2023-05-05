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

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.UnityDots_GenerateRefAccessors_Name), 
        DescriptionResourceName = nameof(Strings.UnityDots_GenerateRefAccessors_Description))]
    public class GenerateRefAccessorsAction : IContextAction
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public GenerateRefAccessorsAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }
        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
            Assertion.Assert(node != null, "node != null");
            var classLikeDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
            
            var fix = new GenerateRefAccessorsActionFix(classLikeDeclaration, node);
            
            var action = new IntentionAction(fix, PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id, 
                new SubmenuAnchor(IntentionsAnchors.HighPriorityContextActionsAnchor, SubmenuBehavior.Executable));

            return new[] {action};
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var node = myDataProvider.GetSelectedTreeNode<ICSharpIdentifier>();
            
            var classDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
            if (classDeclaration == null)
                return false;
            
            var fieldDeclaration = node.GetContainingNode<IFieldDeclaration>();
            if (fieldDeclaration == null) 
                return false;
            
            var fieldTypeElement = fieldDeclaration.DeclaredElement?.Type.GetTypeElement();
            if (fieldDeclaration.IsStatic)
                return false;
            
            return fieldTypeElement.IsClrName(KnownTypes.RefRO) 
                   || fieldTypeElement.IsClrName(KnownTypes.RefRW)
                   || fieldTypeElement.DerivesFrom(KnownTypes.IAspect);
        }
    }
}