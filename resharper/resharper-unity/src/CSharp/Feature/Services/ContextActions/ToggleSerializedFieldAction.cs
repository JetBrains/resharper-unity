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
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Toggle 'SerializeField' and 'NonSerialized' attributes on fields",
        Description = "Toggles a field in a Unity type between serialized and non-serialized. If the field is non-public, the 'UnityEngine.SerializeField' attribute is added. If the field is already serialized, the attribute is removed, and for public fields, the 'NonSerialized' field is added.")]
    public class ToggleSerializedFieldAction : IContextAction
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);

        private readonly ICSharpContextActionDataProvider myDataProvider;

        public ToggleSerializedFieldAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            var multipleFieldDeclaration = MultipleFieldDeclarationNavigator.GetByDeclarator(fieldDeclaration);
            if (multipleFieldDeclaration == null)
                return EmptyList<IntentionAction>.Enumerable;

            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
            var isSerialized = unityApi.IsSerialisedField(fieldDeclaration.DeclaredElement);

            if (multipleFieldDeclaration.Declarators.Count == 1)
            {
                return new ToggleSerializedFieldAll(multipleFieldDeclaration, myDataProvider.PsiModule,
                    myDataProvider.ElementFactory, isSerialized).ToContextActionIntentions();
            }

            return new[]
            {
                new ToggleSerializedFieldOne(fieldDeclaration, myDataProvider.PsiModule, myDataProvider.ElementFactory,
                    isSerialized).ToContextActionIntention(ourSubmenuAnchor),
                new ToggleSerializedFieldAll(multipleFieldDeclaration, myDataProvider.PsiModule,
                    myDataProvider.ElementFactory, isSerialized).ToContextActionIntention(ourSubmenuAnchor)
            };
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!myDataProvider.Project.IsUnityProject())
                return false;

            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            if (fieldDeclaration == null)
                return false;

            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
            return unityApi.IsUnityType(fieldDeclaration.GetContainingTypeDeclaration()?.DeclaredElement);
        }

        private class ToggleSerializedFieldAll : BulbActionBase
        {
            private readonly IFieldDeclaration myFieldDeclaration;
            private readonly IMultipleFieldDeclaration myMultipleFieldDeclaration;
            private readonly IPsiModule myModule;
            private readonly CSharpElementFactory myElementFactory;
            private readonly bool myIsSerialized;

            public ToggleSerializedFieldAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
                CSharpElementFactory elementFactory, bool isSerialized)
            {
                myMultipleFieldDeclaration = multipleFieldDeclaration;
                myFieldDeclaration = (IFieldDeclaration) multipleFieldDeclaration.Declarators[0];
                myModule = module;
                myElementFactory = elementFactory;
                myIsSerialized = isSerialized;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                if (myIsSerialized)
                {
                    AttributeUtil.RemoveAttributeFromAllDeclarations(myFieldDeclaration, KnownTypes.SerializeField);
                    if (myFieldDeclaration.GetAccessRights() == AccessRights.PUBLIC)
                    {
                        AttributeUtil.AddAttributeToAllDeclarations(myFieldDeclaration,
                            PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS, myModule, myElementFactory);
                    }
                }
                else
                {
                    if (myFieldDeclaration.IsStatic)
                        ModifiersUtil.SetStatic(myMultipleFieldDeclaration, false);
                    if (myFieldDeclaration.IsReadonly)
                        ModifiersUtil.SetReadonly(myMultipleFieldDeclaration, false);

                    AttributeUtil.RemoveAttributeFromAllDeclarations(myFieldDeclaration,
                        PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS);
                    if (myFieldDeclaration.GetAccessRights() != AccessRights.PUBLIC)
                    {
                        AttributeUtil.AddAttributeToAllDeclarations(myFieldDeclaration, KnownTypes.SerializeField,
                            myModule, myElementFactory);
                    }
                }

                return null;
            }

            public override string Text
            {
                get
                {
                    var targetDescription = myMultipleFieldDeclaration.Declarators.Count > 1 ? "all fields" : "field";

                    if (myFieldDeclaration.IsStatic && myFieldDeclaration.IsReadonly)
                        return $"Make {targetDescription} serialized (remove static and readonly)";
                    if (myFieldDeclaration.IsStatic)
                        return $"Make {targetDescription} serialized (remove static)";
                    if (myFieldDeclaration.IsReadonly)
                        return $"Make {targetDescription} serialized (remove readonly)";

                    if (!myIsSerialized && myMultipleFieldDeclaration.Declarators.Count == 1)
                        return "To serialized field";

                    return myIsSerialized
                        ? $"Make {targetDescription} non-serialized"
                        : $"Make {targetDescription} serialized";
                }
            }
        }

        private class ToggleSerializedFieldOne : BulbActionBase
        {
            private readonly IFieldDeclaration myFieldDeclaration;
            private readonly IPsiModule myModule;
            private readonly CSharpElementFactory myElementFactory;
            private readonly bool myIsSerialized;

            public ToggleSerializedFieldOne(IFieldDeclaration fieldDeclaration, IPsiModule module,
                CSharpElementFactory elementFactory, bool isSerialized)
            {
                myFieldDeclaration = fieldDeclaration;
                myModule = module;
                myElementFactory = elementFactory;
                myIsSerialized = isSerialized;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                if (myIsSerialized)
                {
                    AttributeUtil.RemoveAttributeFromSingleDeclaration(myFieldDeclaration, KnownTypes.SerializeField);
                    if (myFieldDeclaration.GetAccessRights() == AccessRights.PUBLIC)
                    {
                        AttributeUtil.AddAttributeToSingleDeclaration(myFieldDeclaration,
                            PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS, myModule, myElementFactory);
                    }
                }
                else
                {
                    if (myFieldDeclaration.IsStatic)
                        myFieldDeclaration.SetStatic(false);

                    if (myFieldDeclaration.IsReadonly)
                        myFieldDeclaration.SetReadonly(false);

                    AttributeUtil.RemoveAttributeFromSingleDeclaration(myFieldDeclaration,
                        PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS);
                    if (myFieldDeclaration.GetAccessRights() != AccessRights.PUBLIC)
                    {
                        AttributeUtil.AddAttributeToSingleDeclaration(myFieldDeclaration, KnownTypes.SerializeField,
                            myModule, myElementFactory);
                    }
                }

                return null;
            }

            public override string Text
            {
                get
                {
                    if (myFieldDeclaration.IsStatic && myFieldDeclaration.IsReadonly)
                    {
                        return
                            $"Make field '{myFieldDeclaration.DeclaredName}' serialized (remove static and readonly)";
                    }

                    if (myFieldDeclaration.IsStatic)
                        return $"Make field '{myFieldDeclaration.DeclaredName}' serialized (remove static)";
                    if (myFieldDeclaration.IsReadonly)
                        return $"Make field '{myFieldDeclaration.DeclaredName}' serialized (remove readonly)";

                    return myIsSerialized
                        ? $"Make field '{myFieldDeclaration.DeclaredName}' non-serialized"
                        : $"Make field '{myFieldDeclaration.DeclaredName}' serialized";
                }
            }
        }
    }
}