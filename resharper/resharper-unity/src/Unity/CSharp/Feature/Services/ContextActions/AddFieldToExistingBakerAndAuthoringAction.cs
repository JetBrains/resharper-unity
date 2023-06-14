#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings),
        NameResourceName = nameof(Strings.UnityDots_AddFieldToExistingBakerAndAuthoring_Description),
        DescriptionResourceName = nameof(Strings.UnityDots_AddFieldToExistingBakerAndAuthoring_Description))]
    public class AddFieldToExistingBakerAndAuthoringAction : IContextAction
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;
        private readonly IFieldDeclaration? myFieldDeclaration;
        private readonly ITypeElement? myComponentDeclaredType;
        private readonly IClassLikeDeclaration? myComponentClassDeclaration;

        public AddFieldToExistingBakerAndAuthoringAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
            var node = myDataProvider.GetSelectedTreeNode<ITreeNode>();
            myFieldDeclaration = node?.GetContainingNode<IFieldDeclaration>();
            myComponentDeclaredType = myFieldDeclaration?.DeclaredElement?.ContainingType;
            myComponentClassDeclaration = myFieldDeclaration?.GetContainingNode<IClassLikeDeclaration>();
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (myFieldDeclaration == null || myComponentDeclaredType == null || myComponentClassDeclaration == null)
                return EnumerableCollection<IntentionAction>.Empty;

            var fix = new AddFieldToExistingBakerAndAuthoring(myFieldDeclaration,
                myComponentClassDeclaration);

            var action = new IntentionAction(fix, PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id,
                new SubmenuAnchor(IntentionsAnchors.HighPriorityContextActionsAnchor, SubmenuBehavior.Executable));

            return new[] { action };
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!DotsUtils.IsUnityProjectWithEntitiesPackage(myDataProvider.PsiFile))
                return false;

            return myComponentDeclaredType.DerivesFrom(KnownTypes.IComponentData);
        }
    }

    internal class AddFieldToExistingBakerAndAuthoring : BulbActionBase
    {
        private readonly IFieldDeclaration myFieldDeclaration;
        private readonly IClassLikeDeclaration myComponentClassDeclaration;

        public AddFieldToExistingBakerAndAuthoring(IFieldDeclaration fieldDeclaration, IClassLikeDeclaration componentClassDeclaration)
        {
            myFieldDeclaration = fieldDeclaration;
            myComponentClassDeclaration = componentClassDeclaration;
        }

        public override string Text => string.Format(Strings.UnityDots_AddFieldToExistingBakerAndAuthoring_Text,
            myFieldDeclaration.DeclaredName);

        protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var (bakerBaseTypeElement, _) = TypeFactory.CreateTypeByCLRName(KnownTypes.Baker,
                NullableAnnotation.Unknown, myFieldDeclaration.GetPsiModule());
            var bakers = new List<ITypeElement>();

            var psiServices = solution.GetPsiServices();
            var finder = psiServices.Finder;

            using (var spi = new SubProgressIndicator(progress, 1))
            {
                finder.FindInheritors(bakerBaseTypeElement, bakers.ConsumeDeclaredElements(), spi);
            }

            if (bakers.Count == 0)
                return null;

            var componentDeclaredElement = myFieldDeclaration.GetContainingTypeDeclaration()?.DeclaredElement;
            if (componentDeclaredElement == null)
                return null;

            IReference[]? componentReferences;
            using (var spi = new SubProgressIndicator(progress, 2))
            {
                componentReferences = finder.FindReferences(componentDeclaredElement, bakers.UnionSearchDomains(), spi);
            }

            if (componentReferences.IsEmpty())
                return null;

            using (var spi = new SubProgressIndicator(progress, 3))
            {
                spi.Start(componentReferences.Length);
                for (var index = 0; index < componentReferences.Length; index++)
                {
                    spi.Advance(index);

                    var componentReference = componentReferences[index];

                    var addComponentExpression =
                        componentReference.GetTreeNode().GetContainingNode<IInvocationExpression>();
                    if (addComponentExpression == null)
                        continue;

                    if (!addComponentExpression.IsIBakerAddComponentMethod() &&
                        !addComponentExpression.IsIBakerAddComponentObjectMethod())
                        continue;

                    var bakerClassLikeDeclaration = addComponentExpression.GetContainingNode<IClassLikeDeclaration>();

                    var selectedBaker = bakerClassLikeDeclaration?.DeclaredElement;

                    if (selectedBaker == null)
                        continue;

                    var selectedAuthoringComponent =
                        GenerateBakerAndAuthoringActionBuilder.GetSelectedAuthoringComponent(selectedBaker);

                    if (selectedAuthoringComponent == null)
                        continue;

                    var factory = CSharpElementFactory.GetInstance(addComponentExpression);

                    var generationParameters = new GenerateBakerAndAuthoringActionBuilder.GenerationParameters(
                        new List<IField> { myFieldDeclaration.DeclaredElement },
                        myComponentClassDeclaration,
                        selectedAuthoringComponent,
                        selectedBaker,
                        true,
                        factory,
                        addComponentExpression.PsiModule,
                        false);

                    GenerateBakerAndAuthoringActionBuilder.GenerateBakerAndAuthoring(generationParameters);
                }
            }

            return null;
        }
    }
}