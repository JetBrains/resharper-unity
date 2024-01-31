using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(GroupType = typeof(CSharpUnityContextActions),
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.ToggleSerializedFieldAction_Name), 
        DescriptionResourceName = nameof(Strings.ToggleSerializedFieldAction_Description))]
    public class ToggleSerializedFieldAction : IContextAction
    {
        private static readonly SubmenuAnchor ourSubmenuAnchor =
            new(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);

        private readonly ICSharpContextActionDataProvider myDataProvider;

        public ToggleSerializedFieldAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            var multipleFieldDeclaration = MultipleFieldDeclarationNavigator.GetByDeclarator(fieldDeclaration);
            if (fieldDeclaration == null || multipleFieldDeclaration == null)
                return EmptyList<IntentionAction>.Enumerable;

            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
            var isSerialized = unityApi.IsSerialisedField(fieldDeclaration.DeclaredElement) == SerializedFieldStatus.SerializedField;

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
            
            var isFieldInsideUnityType = unityApi.IsUnityType(fieldDeclaration.GetContainingTypeDeclaration()?.DeclaredElement);
            if (!isFieldInsideUnityType)
                return false;

            if (fieldDeclaration.DeclaredElement == null)
                return false;

            var unityType = unityApi.IsFieldTypeSerializable(fieldDeclaration.DeclaredElement,
                hasSerializeReference: false, useSwea: true);
            
            return unityType == SerializedFieldStatus.SerializedField;
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

            protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                if (myIsSerialized)
                {
                    AttributeUtil.RemoveAttributeFromAllDeclarations(myFieldDeclaration, KnownTypes.SerializeField);
                    if (myFieldDeclaration.GetAccessRights() == AccessRights.PUBLIC)
                    {
                        AttributeUtil.AddAttributeToEntireDeclaration(myMultipleFieldDeclaration,
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
                        AttributeUtil.AddAttributeToEntireDeclaration(myMultipleFieldDeclaration,
                            KnownTypes.SerializeField, myModule, myElementFactory);
                    }
                }

                return null;
            }

            public override string Text
            {
                get
                {
                    var targetDescription = myMultipleFieldDeclaration.Declarators.Count > 1 ? Strings.ToggleSerializedFieldAll_Text_all_fields : Strings.ToggleSerializedFieldAll_Text_field;

                    if (myFieldDeclaration.IsStatic && myFieldDeclaration.IsReadonly)
                        return string.Format(Strings.ToggleSerializedFieldAll_Text_Make__0__serialized__remove_static_and_readonly_, targetDescription);
                    if (myFieldDeclaration.IsStatic)
                        return string.Format(Strings.ToggleSerializedFieldAll_Text_Make__0__serialized__remove_static_, targetDescription);
                    if (myFieldDeclaration.IsReadonly)
                        return string.Format(Strings.ToggleSerializedFieldAll_Text_Make__0__serialized__remove_readonly_, targetDescription);

                    if (!myIsSerialized && myMultipleFieldDeclaration.Declarators.Count == 1)
                        return Strings.ToggleSerializedFieldAll_Text_To_serialized_field;

                    return myIsSerialized
                        ? string.Format(Strings.ToggleSerializedFieldAll_Text_Make__0__non_serialized, targetDescription)
                        : string.Format(Strings.ToggleSerializedFieldAll_Text_Make__0__serialized, targetDescription);
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

            protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
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
                            string.Format(Strings.ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_static_and_readonly_, myFieldDeclaration.DeclaredName);
                    }

                    if (myFieldDeclaration.IsStatic)
                        return string.Format(Strings.ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_static_, myFieldDeclaration.DeclaredName);
                    if (myFieldDeclaration.IsReadonly)
                        return string.Format(Strings.ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_readonly_, myFieldDeclaration.DeclaredName);

                    return myIsSerialized
                        ? string.Format(Strings.ToggleSerializedFieldOne_Text_Make_field___0___non_serialized, myFieldDeclaration.DeclaredName)
                        : string.Format(Strings.ToggleSerializedFieldOne_Text_Make_field___0___serialized, myFieldDeclaration.DeclaredName);
                }
            }
        }
    }
}
