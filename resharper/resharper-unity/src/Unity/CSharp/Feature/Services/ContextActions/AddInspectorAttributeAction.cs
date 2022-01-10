using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Positions;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    public abstract class AddInspectorAttributeAction : IContextAction
    {
        protected static readonly IAnchorPosition LayoutPosition = AnchorPosition.BeforePosition;
        protected static readonly IAnchorPosition AnnotationPosition = LayoutPosition.GetNext();
        protected static readonly SubmenuAnchor BaseAnchor =
            new SubmenuAnchor(IntentionsAnchors.LowPriorityContextActionsAnchor, SubmenuBehavior.Static("Modify Inspector attributes"));

        private readonly ICSharpContextActionDataProvider myDataProvider;
        private readonly IAnchor myAnchor;

        protected AddInspectorAttributeAction(ICSharpContextActionDataProvider dataProvider, IAnchor anchor)
        {
            myDataProvider = dataProvider;
            myAnchor = anchor;
        }

        protected abstract IClrTypeName AttributeTypeName { get; }
        protected virtual bool IsRemoveActionAvailable => false;
        protected virtual bool SupportsSingleDeclarationOnly => false;

        // A layout attribute is conceptually applied "in between" fields. As in, Header and Space are added before the
        // currently selected field, while Range, Tooltip and HideInInspector are applied *to* a field. This changes
        // the text of the bulb actions and the default action shown on multiple field declarations.
        protected abstract bool IsLayoutAttribute { get; }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var selectedFieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            var multipleFieldDeclaration = MultipleFieldDeclarationNavigator.GetByDeclarator(selectedFieldDeclaration);
            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();

            if (!unityApi.IsSerialisedField(selectedFieldDeclaration?.DeclaredElement) || multipleFieldDeclaration == null)
                return EmptyList<IntentionAction>.Enumerable;

            var existingAttribute = selectedFieldDeclaration.GetAttribute(AttributeTypeName);

            var actionToApplyToEntireDeclaration = GetActionToApplyToEntireFieldDeclaration(multipleFieldDeclaration,
                    selectedFieldDeclaration, myDataProvider.PsiModule, myDataProvider.ElementFactory,
                    existingAttribute)
                .ToContextActionIntention(myAnchor);

            // If we only have a single field in the declaration, then use the default action ("Add 'Attr'")
            // This is the most likely case
            if (multipleFieldDeclaration.Declarators.Count == 1)
                return new[] {actionToApplyToEntireDeclaration};

            var actionToExtractAndApply = GetActionToExtractAndApplyToSingleField(multipleFieldDeclaration,
                    selectedFieldDeclaration, myDataProvider.PsiModule, myDataProvider.ElementFactory,
                    existingAttribute)
                .ToContextActionIntention(myAnchor);

            // Only makes sense to apply to a single attribute, not all. E.g. you can't apply Range to all the fields
            // in a multiple
            if (SupportsSingleDeclarationOnly)
                return new[] {actionToExtractAndApply};

            // Change the order of main menu and submenu. If it's a layout attribute (e.g. 'Space'):
            // "Add 'Attr' before all fields" -> "Add 'Attr' before 'field'"
            // If it's an annotation attribute (e.g. 'Tooltip'):
            // "Add 'Attr' to 'field'" -> "Add 'Attr' to all fields"
            return IsLayoutAttribute
                ? new[] {actionToApplyToEntireDeclaration, actionToExtractAndApply}
                : new[] {actionToExtractAndApply, actionToApplyToEntireDeclaration};
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!myDataProvider.Project.IsUnityProject())
                return false;

            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            if (fieldDeclaration == null || !unityApi.IsSerialisedField(fieldDeclaration.DeclaredElement))
                return false;

            var existingAttribute = fieldDeclaration.GetAttribute(AttributeTypeName);
            if (existingAttribute != null && !IsRemoveActionAvailable)
                return false;

            // Only for UnityObject types, not [Serialized] types
            var classDeclaration = fieldDeclaration.GetContainingTypeDeclaration();
            var classElement = classDeclaration?.DeclaredElement;
            return classElement.DerivesFromMonoBehaviour() || classElement.DerivesFromScriptableObject();
        }

        private BulbActionBase GetActionToApplyToEntireFieldDeclaration(
            IMultipleFieldDeclaration multipleFieldDeclaration,
            IFieldDeclaration selectedFieldDeclaration,
            IPsiModule module,
            CSharpElementFactory elementFactory,
            IAttribute existingAttribute)
        {
            // Don't pass selectedFieldDeclaration to the actions, as we're applying the action to all fields
            // We only have selectedFieldDeclaration to get default attribute values, and even that's not actually used
            if (existingAttribute != null)
                return new RemoveAttributeAction(multipleFieldDeclaration, null, existingAttribute);

            var attributeValues = GetAttributeValues(module, selectedFieldDeclaration);
            return new AddAttributeAction(multipleFieldDeclaration, null, AttributeTypeName, attributeValues,
                IsLayoutAttribute, module, elementFactory);
        }

        private BulbActionBase GetActionToExtractAndApplyToSingleField(
            IMultipleFieldDeclaration multipleFieldDeclaration,
            IFieldDeclaration selectedFieldDeclaration,
            IPsiModule module,
            CSharpElementFactory elementFactory,
            IAttribute existingAttribute)
        {
            if (existingAttribute != null)
                return new RemoveAttributeAction(multipleFieldDeclaration, selectedFieldDeclaration, existingAttribute);

            var attributeValues = GetAttributeValues(module, selectedFieldDeclaration);
            return new AddAttributeAction(multipleFieldDeclaration, selectedFieldDeclaration, AttributeTypeName,
                attributeValues, IsLayoutAttribute, module, elementFactory);
        }

        [NotNull]
        protected virtual AttributeValue[] GetAttributeValues(IPsiModule module,
                                                              IFieldDeclaration selectedFieldDeclaration) =>
            EmptyArray<AttributeValue>.Instance;

        private class AddAttributeAction : BulbActionBase
        {
            private readonly IMultipleFieldDeclaration myMultipleFieldDeclaration;
            [CanBeNull] private readonly IFieldDeclaration mySelectedFieldDeclaration;
            private readonly IPsiModule myModule;
            private readonly CSharpElementFactory myElementFactory;
            private readonly IClrTypeName myAttributeTypeName;
            private readonly AttributeValue[] myAttributeValues;
            private readonly bool myIsLayoutAttribute;

            public AddAttributeAction(IMultipleFieldDeclaration multipleFieldDeclaration,
                                      [CanBeNull] IFieldDeclaration selectedFieldDeclaration,
                                      IClrTypeName attributeTypeName,
                                      [NotNull] AttributeValue[] attributeValues,
                                      bool isLayoutAttribute,
                                      IPsiModule module, CSharpElementFactory elementFactory)
            {
                myMultipleFieldDeclaration = multipleFieldDeclaration;
                mySelectedFieldDeclaration = selectedFieldDeclaration;
                myAttributeTypeName = attributeTypeName;
                myAttributeValues = attributeValues;
                myIsLayoutAttribute = isLayoutAttribute;
                myModule = module;
                myElementFactory = elementFactory;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(
                ISolution solution, IProgressIndicator progress)
            {
                IAttribute attribute;

                if (mySelectedFieldDeclaration == null)
                {
                    attribute = AttributeUtil.AddAttributeToEntireDeclaration(myMultipleFieldDeclaration,
                        myAttributeTypeName, myAttributeValues, null, myModule, myElementFactory);
                }
                else
                {
                    attribute = AttributeUtil.AddAttributeToSingleDeclaration(mySelectedFieldDeclaration,
                        myAttributeTypeName, myAttributeValues, null, myModule, myElementFactory);
                }

                if (myAttributeValues.Length == 0)
                    return null;

                return attribute.CreateHotspotSession();
            }

            public override string Text
            {
                get
                {
                    var displayName = myAttributeTypeName.ShortName.RemoveEnd("Attribute");
                    if (myMultipleFieldDeclaration.Declarators.Count == 1)
                        return $"Add '{displayName}'";

                    // Layout attribute is about position between fields, not being applied to fields
                    var preposition = myIsLayoutAttribute ? "before" : "to";
                    return mySelectedFieldDeclaration != null
                        ? $"Add '{displayName}' {preposition} '{mySelectedFieldDeclaration.DeclaredName}'"
                        : $"Add '{displayName}' {preposition} all fields";
                }
            }
        }

        private class RemoveAttributeAction : BulbActionBase
        {
            private readonly IMultipleFieldDeclaration myMultipleFieldDeclaration;
            [CanBeNull] private readonly IFieldDeclaration mySelectedFieldDeclaration;
            private readonly IAttribute myExistingAttribute;

            public RemoveAttributeAction(IMultipleFieldDeclaration multipleFieldDeclaration,
                                         [CanBeNull] IFieldDeclaration selectedFieldDeclaration,
                                         IAttribute existingAttribute)
            {
                myMultipleFieldDeclaration = multipleFieldDeclaration;
                mySelectedFieldDeclaration = selectedFieldDeclaration;
                myExistingAttribute = existingAttribute;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(
                ISolution solution, IProgressIndicator progress)
            {
                if (mySelectedFieldDeclaration != null)
                {
                    // This will split any multiple field declarations
                    mySelectedFieldDeclaration.RemoveAttribute(myExistingAttribute);
                }
                else
                {
                    var fieldDeclaration = (IFieldDeclaration) myMultipleFieldDeclaration.Declarators[0];
                    CSharpSharedImplUtil.RemoveAttribute(fieldDeclaration, myExistingAttribute);
                }

                return null;
            }

            public override string Text
            {
                get
                {
                    var displayName = myExistingAttribute.Name.ShortName;
                    if (myMultipleFieldDeclaration.Declarators.Count == 1)
                        return $"Remove '{displayName}'";

                    if (mySelectedFieldDeclaration != null)
                    {
                        return $"Remove '{displayName}' from '{mySelectedFieldDeclaration.DeclaredName}'";
                    }

                    return $"Remove '{displayName}' from all fields";
                }
            }
        }
    }
}