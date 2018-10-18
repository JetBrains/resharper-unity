using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Toggle 'HideInInspector' attribute on fields",
        Description =
            "Adds or removes the 'HideInInspector' attribute on a Unity serialized field, removing the field from the Inspector window.")]
    public class ToggleHideInInspectorAttributeAction : IContextAction
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);

        private readonly ICSharpContextActionDataProvider myDataProvider;

        public ToggleHideInInspectorAttributeAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            var multipleFieldDeclaration = MultipleFieldDeclarationNavigator.GetByDeclarator(fieldDeclaration);
            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();

            if (!unityApi.IsSerialisedField(fieldDeclaration?.DeclaredElement) || multipleFieldDeclaration == null)
                return EmptyList<IntentionAction>.Enumerable;

            var existingAttribute = AttributeUtil.GetAttribute(fieldDeclaration, KnownTypes.HideInInspector);

            if (multipleFieldDeclaration.Declarators.Count == 1)
            {
                return new ToggleHideInInspectorAll(multipleFieldDeclaration, myDataProvider.PsiModule,
                    myDataProvider.ElementFactory, existingAttribute).ToContextActionIntentions();
            }

            return new[]
            {
                new ToggleHideInInspectorOne(fieldDeclaration, myDataProvider.PsiModule, myDataProvider.ElementFactory,
                    existingAttribute).ToContextActionIntention(ourSubmenuAnchor),
                new ToggleHideInInspectorAll(multipleFieldDeclaration, myDataProvider.PsiModule,
                    myDataProvider.ElementFactory, existingAttribute).ToContextActionIntention(ourSubmenuAnchor)
            };
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!myDataProvider.Project.IsUnityProject())
                return false;

            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            return unityApi.IsSerialisedField(fieldDeclaration?.DeclaredElement);
        }

        private class ToggleHideInInspectorAll : BulbActionBase
        {
            private readonly IMultipleFieldDeclaration myMultipleFieldDeclaration;
            private readonly IPsiModule myModule;
            private readonly CSharpElementFactory myElementFactory;
            private readonly IAttribute myExistingAttribute;

            public ToggleHideInInspectorAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
                CSharpElementFactory elementFactory, IAttribute existingAttribute)
            {
                myMultipleFieldDeclaration = multipleFieldDeclaration;
                myModule = module;
                myElementFactory = elementFactory;
                myExistingAttribute = existingAttribute;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var fieldDeclaration = (IFieldDeclaration) myMultipleFieldDeclaration.Declarators[0];
                if (myExistingAttribute != null)
                    CSharpSharedImplUtil.RemoveAttribute(fieldDeclaration, myExistingAttribute);
                else
                {
                    AttributeUtil.AddAttributeToAllDeclarations(fieldDeclaration, KnownTypes.HideInInspector,
                        myModule, myElementFactory);
                }

                return null;
            }

            public override string Text
            {
                get
                {
                    if (myExistingAttribute != null)
                    {
                        return myMultipleFieldDeclaration.Declarators.Count > 1
                            ? "Remove 'HideInInspector' attribute from all fields"
                            : "Remove 'HideInInspector' attribute";
                    }
                    var targetDescription = myMultipleFieldDeclaration.Declarators.Count > 1 ? "all fields" : "field";
                    return $"Annotate {targetDescription} with 'HideInInspector' attribute";
                }
            }
        }

        private class ToggleHideInInspectorOne : BulbActionBase
        {
            private readonly IFieldDeclaration myFieldDeclaration;
            private readonly IPsiModule myPsiModule;
            private readonly CSharpElementFactory myElementFactory;
            private readonly IAttribute myExistingAttribute;

            public ToggleHideInInspectorOne(IFieldDeclaration fieldDeclaration, IPsiModule psiModule,
                CSharpElementFactory elementFactory, IAttribute existingAttribute)
            {
                myFieldDeclaration = fieldDeclaration;
                myPsiModule = psiModule;
                myElementFactory = elementFactory;
                myExistingAttribute = existingAttribute;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution,
                IProgressIndicator progress)
            {
                if (myExistingAttribute != null)
                    myFieldDeclaration.RemoveAttribute(myExistingAttribute);
                else
                {
                    AttributeUtil.AddAttributeToSingleDeclaration(myFieldDeclaration, KnownTypes.HideInInspector,
                        myPsiModule, myElementFactory);
                }

                return null;
            }

            public override string Text => myExistingAttribute != null
                ? $"Remove 'HideInInspector' attribute from '{myFieldDeclaration.DeclaredName}'"
                : $"Annotate field '{myFieldDeclaration.DeclaredName}' with 'HideInInspector' attribute";
        }
    }
}