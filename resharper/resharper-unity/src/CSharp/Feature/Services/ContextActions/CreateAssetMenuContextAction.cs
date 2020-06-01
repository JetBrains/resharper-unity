using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Add 'CreateAssetMenu' attribute",
        Description = "Adds the 'CreateAssetMenu' attribute to a scriptable object. " +
                      "This marks a 'ScriptableObject'-derived type to be automatically listed in " +
                      "Unity's 'Assets/Create' menu, so that instances of the type can be easily created " +
                      "and stored in the project as '.asset' files")]
    public class CreateAssetMenuContextAction : IContextAction
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.HighPriorityContextActionsAnchor, SubmenuBehavior.Executable);

        private readonly ICSharpContextActionDataProvider myDataProvider;

        public CreateAssetMenuContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            return new[]
            {
                new CreateAssetMenuAction(myDataProvider.GetSelectedElement<IClassLikeDeclaration>(), myDataProvider.ElementFactory, myDataProvider.PsiModule).ToContextActionIntention(ourSubmenuAnchor)
            };
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var identifier = myDataProvider.GetSelectedElement<ICSharpIdentifier>();

            var classLikeDeclaration = ClassLikeDeclarationNavigator.GetByNameIdentifier(identifier);
            if (classLikeDeclaration == null)
                return false;
            
            var existingAttribute = classLikeDeclaration.GetAttribute(KnownTypes.CreateAssetMenuAttribute);
            return existingAttribute == null && UnityApi.IsDescendantOfScriptableObject(classLikeDeclaration.DeclaredElement);
        }

        private class CreateAssetMenuAction : BulbActionBase
        {
            private readonly IClassLikeDeclaration myClassLikeDeclaration;
            private readonly IPsiModule myModule;
            private readonly CSharpElementFactory myElementFactory;

            public CreateAssetMenuAction(IClassLikeDeclaration classLikeDeclaration, CSharpElementFactory elementFactory, IPsiModule module)
            {
                myClassLikeDeclaration = classLikeDeclaration;
                myModule = module;
                myElementFactory = elementFactory;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var values = new[]
                {
                    new Pair<string, AttributeValue>("menuName", new AttributeValue(new ConstantValue($"Create {myClassLikeDeclaration.DeclaredName}", myModule))),
                    new Pair<string, AttributeValue>("fileName", new AttributeValue(new ConstantValue(myClassLikeDeclaration.DeclaredName, myModule))),
                    new Pair<string, AttributeValue>("order", new AttributeValue(new ConstantValue(0, myModule))),
                };
                
                var attribute = AttributeUtil.AddAttributeToSingleDeclaration(myClassLikeDeclaration, KnownTypes.CreateAssetMenuAttribute, EmptyArray<AttributeValue>.Instance, 
                    values, myModule, myElementFactory);
                
                return attribute.CreateHotspotSession();
            }

            public override string Text => "Add to Unity's 'Assets/Create' menu";
        }
    }
}