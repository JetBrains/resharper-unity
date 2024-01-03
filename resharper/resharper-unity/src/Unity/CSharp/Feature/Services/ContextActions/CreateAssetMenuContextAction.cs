﻿using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.CreateAssetMenuContextAction_Name), 
        DescriptionResourceName = nameof(Strings.CreateAssetMenuContextAction_Description))]
    public class CreateAssetMenuContextAction : IContextAction
    {
        private static readonly SubmenuAnchor ourSubmenuAnchor =
            new(IntentionsAnchors.HighPriorityContextActionsAnchor, SubmenuBehavior.Executable);

        private readonly ICSharpContextActionDataProvider myDataProvider;

        public CreateAssetMenuContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            // We know this isn't null, because we checked in IsAvailable
            var classLikeDeclaration = myDataProvider.GetSelectedElement<IClassLikeDeclaration>()
                .NotNull("myDataProvider.GetSelectedElement<IClassLikeDeclaration>() != null");
            return new[]
            {
                new CreateAssetMenuAction(classLikeDeclaration, myDataProvider.ElementFactory, myDataProvider.PsiModule)
                    .ToContextActionIntention(ourSubmenuAnchor)
            };
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var identifier = myDataProvider.GetSelectedElement<ICSharpIdentifier>();

            var classLikeDeclaration = ClassLikeDeclarationNavigator.GetByNameIdentifier(identifier);
            if (classLikeDeclaration == null)
                return false;

            var declaredElement = classLikeDeclaration.DeclaredElement;

            if (declaredElement.DerivesFrom(KnownTypes.EditorWindow) || declaredElement.DerivesFrom(KnownTypes.Editor))
                return false;

            var existingAttribute = classLikeDeclaration.GetAttribute(KnownTypes.CreateAssetMenuAttribute);
            return existingAttribute == null && declaredElement.DerivesFromScriptableObject();
        }

        private class CreateAssetMenuAction : BulbActionBase
        {
            private readonly IClassLikeDeclaration myClassLikeDeclaration;
            private readonly IPsiModule myModule;
            private readonly CSharpElementFactory myElementFactory;

            public CreateAssetMenuAction(IClassLikeDeclaration classLikeDeclaration,
                                         CSharpElementFactory elementFactory, IPsiModule module)
            {
                myClassLikeDeclaration = classLikeDeclaration;
                myModule = module;
                myElementFactory = elementFactory;
            }

            protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var fixedArguments = EmptyArray<AttributeValue>.Instance;
                var namedArguments = new[]
                {
                    new Pair<string, AttributeValue>("menuName", new AttributeValue(ConstantValue.String($"Create {myClassLikeDeclaration.DeclaredName}", myModule))),
                    new Pair<string, AttributeValue>("fileName", new AttributeValue(ConstantValue.String(myClassLikeDeclaration.DeclaredName, myModule))),
                    new Pair<string, AttributeValue>("order", new AttributeValue(ConstantValue.Int(0, myModule))),
                };

                var attribute = AttributeUtil.AddAttributeToSingleDeclaration(myClassLikeDeclaration,
                    KnownTypes.CreateAssetMenuAttribute, fixedArguments, namedArguments, myModule,
                    myElementFactory);
                return attribute?.CreateHotspotSession();
            }

            public override string Text => Strings.CreateAssetMenuAction_Text_Add_to_Unity_s__Assets_Create__menu;
        }
    }
}
