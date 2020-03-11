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
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Add 'CreateAssetMenu' attribute for scriptable object",
        Description = "Add 'CreateAssetMenu' attribute for scriptable object which allows to create asset from context menu for that scriptable object")]
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
            var classLikeDeclaration = myDataProvider.GetSelectedElement<IClassLikeDeclaration>();
            if (classLikeDeclaration == null)
                return false;
            
            var existingAttribute = classLikeDeclaration.GetAttribute(KnownTypes.CreateAssetMenu);
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
                
                var attribute = AttributeUtil.AddAttributeToSingleDeclaration(myClassLikeDeclaration, KnownTypes.CreateAssetMenu, EmptyArray<AttributeValue>.Instance, 
                    values, myModule, myElementFactory);
                
                return CreateHotspotSession(attribute);
            }

            private Action<ITextControl> CreateHotspotSession(IAttribute attribute)
            {
                var hotspotsRegistry = new HotspotsRegistry(myClassLikeDeclaration.GetSolution().GetPsiServices());

                var arguments = attribute.PropertyAssignments;
                for (var i = 0; i < arguments.Count; i++)
                {
                    hotspotsRegistry.Register(new ITreeNode[] {arguments[i].Source}, new NameSuggestionsExpression(new[] {""}));
                }

                return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, DocumentOffset.InvalidOffset);
            }

            public override string Text => "Create asset menu";
        }
    }
}