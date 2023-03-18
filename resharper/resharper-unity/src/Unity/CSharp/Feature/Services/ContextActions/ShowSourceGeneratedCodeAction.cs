using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings),
        NameResourceName = nameof(Strings.UnityDots_PartialClassesGeneratedCode_ShowGeneratedCode),
        DescriptionResourceName = nameof(Strings.UnityDots_PartialClassesGeneratedCode_ShowGeneratedCode))]
    public class ShowSourceGeneratedCodeAction : IContextAction
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public ShowSourceGeneratedCodeAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();

            var classDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
            if (classDeclaration == null)
                yield break;

            yield return new OpenDotsSourceGeneratedFileBulbAction(Strings.UnityDots_PartialClassesGeneratedCode_ShowGeneratedCode, classDeclaration)
                .ToContextActionIntention(BulbMenuAnchors.FirstClassContextItems, PsiFeaturesUnsortedThemedIcons.Navigate.Id);
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();

            var classLikeDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
            if (classLikeDeclaration is not { IsPartial: true })
                return false;
            if (classLikeDeclaration.GetSourceFile().IsSourceGeneratedFile())
                return false;
            if (classLikeDeclaration.DeclaredElement?.GetDeclarations().Count <= 1)
                return false;
            
            return node.GetContainingNode<IClassBody>() == null 
                   && classLikeDeclaration.DeclaredElement.IsDotsImplicitlyUsedType();
        }
    }
}