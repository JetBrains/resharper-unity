using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class FormerlySerializedAsAtomicRename : AtomicRenameBase
    {
        private readonly KnownTypesCache myKnownTypesCache;
        private readonly SerializedFieldRenameModel myModel;
        private readonly IDeclaredElementPointer<IDeclaredElement> myPointer;

        public FormerlySerializedAsAtomicRename(IDeclaredElement declaredElement, string newName,
            ISettingsStore settingsStore, KnownTypesCache knownTypesCache)
        {
            myKnownTypesCache = knownTypesCache;
            myModel = new SerializedFieldRenameModel(settingsStore);

            myPointer = declaredElement.CreateElementPointer();
            OldName = declaredElement.ShortName;
            NewName = newName;
        }

        public override IRefactoringPage CreateRenamesConfirmationPage(IRenameWorkflow renameWorkflow,
            IProgressIndicator pi)
        {
            // hide confirmation page only, refactoring should update shared document too otherwise
            // we will get inconsistent change modification message box
            if (myModel.DontShowPopup)
                return null;

            return new FormerlySerializedAsRefactoringPage(
                ((RefactoringWorkflowBase) renameWorkflow).WorkflowExecuterLifetime, myModel);
        }

        public override void Rename(IRenameRefactoring executer, IProgressIndicator pi, bool hasConflictsWithDeclarations,
            IRefactoringDriver driver)
        {
            if (!myModel.ShouldAddFormerlySerializedAs)
                return;

            var fieldDeclaration = GetFieldDeclaration(myPointer.FindDeclaredElement() as IField);
            if (fieldDeclaration == null)
                return;

            RemoveExistingAttributesWithNewName(fieldDeclaration);

            if (HasExistingFormerlySerializedAsAttribute(fieldDeclaration))
            {
                // Make sure textual occurrence rename doesn't rename the existing attribute parameter
                RemoveFromTextualOccurrences(executer, fieldDeclaration);
                return;
            }

            var attribute = CreateFormerlySerializedAsAttribute(fieldDeclaration);
            if (attribute != null)
                fieldDeclaration.AddAttributeAfter(attribute, null);
        }

        private static IFieldDeclaration? GetFieldDeclaration(IField? field)
        {
            var declarations = field?.GetDeclarations();
            if (declarations?.Count == 1)
                return declarations[0] as IFieldDeclaration;
            return null;
        }

        public override IDeclaredElement NewDeclaredElement =>
            myPointer.FindDeclaredElement().NotNull("myPointer.FindDeclaredElement() != null");

        public override string NewName { get; }
        public override string OldName { get; }

        public override IDeclaredElement PrimaryDeclaredElement =>
            myPointer.FindDeclaredElement().NotNull("myPointer.FindDeclaredElement() != null");

        public override IList<IDeclaredElement>? SecondaryDeclaredElements => null;

        private void RemoveExistingAttributesWithNewName(IFieldDeclaration fieldDeclaration)
        {
            var attributes = GetExistingFormerlySerializedAsAttributes(fieldDeclaration, NewName);
            foreach (var attribute in attributes)
                fieldDeclaration.RemoveAttribute(attribute);
        }

        private bool HasExistingFormerlySerializedAsAttribute(IFieldDeclaration fieldDeclaration)
        {
            var attributes = GetExistingFormerlySerializedAsAttributes(fieldDeclaration, OldName);
            return attributes.Count > 0;
        }

        private FrugalLocalList<IAttribute> GetExistingFormerlySerializedAsAttributes(
            IFieldDeclaration fieldDeclaration, string nameArgument)
        {
            var list = new FrugalLocalList<IAttribute>();
            foreach (var attribute in fieldDeclaration.AttributesEnumerable)
            {
                var attributeTypeElement = attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement;
                if (attributeTypeElement == null)
                    continue;

                if (Equals(attributeTypeElement.GetClrName(), KnownTypes.FormerlySerializedAsAttribute))
                {
                    var attributeInstance = attribute.GetAttributeInstance();
                    var nameParameter = attributeInstance.PositionParameter(0);
                    if (nameParameter.IsConstant && nameParameter.ConstantValue.IsString() &&
                        (string) nameParameter.ConstantValue.Value! == nameArgument)
                    {
                        list.Add(attribute);
                    }
                }
            }

            return list;
        }

        private void RemoveFromTextualOccurrences(IRenameRefactoring executor, IFieldDeclaration fieldDeclaration)
        {
            if (!(executor.Workflow is RenameWorkflow workflow))
                return;

            var attributes = fieldDeclaration.Attributes;
            if (attributes.Count == 0)
                return;

            var attribute = attributes[0];
            var attributeSectionList = AttributeSectionListNavigator.GetByAttribute(attribute);
            if (attributeSectionList == null)
                return;

            var attributesRange = attributeSectionList.GetDocumentRange();

            foreach (var occurrence in workflow.DataModel.ActualOccurrences ?? EmptyList<TextOccurrenceRenameMarker>.InstanceList)
            {
                if (!occurrence.Included)
                    continue;


                var occurrenceRange = occurrence.Marker.DocumentRange;
                if (attributesRange.Contains(occurrenceRange))
                {
                    occurrence.Included = false;
                    break;
                }
            }
        }

        private IAttribute? CreateFormerlySerializedAsAttribute(ITreeNode owningNode)
        {
            var module = owningNode.GetPsiModule();
            var elementFactory = CSharpElementFactory.GetInstance(owningNode);
            var attributeType = myKnownTypesCache.GetByClrTypeName(KnownTypes.FormerlySerializedAsAttribute, module);
            var attributeTypeElement = attributeType.GetTypeElement();
            if (attributeTypeElement == null)
                return null;

            return elementFactory.CreateAttribute(attributeTypeElement, new[]
                {
                    new AttributeValue(ConstantValue.String(OldName, module))
                },
                EmptyArray<Pair<string, AttributeValue>>.Instance);
        }
    }
}