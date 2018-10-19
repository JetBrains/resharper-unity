using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class FormerlySerializedAsAtomicRename : AtomicRenameBase
    {
        private readonly SerializedFieldRenameModel myModel;
        private readonly IDeclaredElementPointer<IDeclaredElement> myPointer;

        public FormerlySerializedAsAtomicRename(IDeclaredElement declaredElement, string newName, ISettingsStore settingsStore)
        {
            myModel = new SerializedFieldRenameModel(settingsStore);

            myPointer = declaredElement.CreateElementPointer();
            OldName = declaredElement.ShortName;
            NewName = newName;
        }

        public override IRefactoringPage CreateRenamesConfirmationPage(IRenameWorkflow renameWorkflow,
            IProgressIndicator pi)
        {
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

            var attribute = CreateFormerlySerializedAsAttribute(fieldDeclaration.GetPsiModule());
            if (attribute != null)
                fieldDeclaration.AddAttributeAfter(attribute, null);
        }

        private IFieldDeclaration GetFieldDeclaration(IField field)
        {
            var declarations = field.GetDeclarations();
            Assertion.Assert(declarations.Count == 1, "declarations.Count == 1");
            return declarations[0] as IFieldDeclaration;
        }

        public override IDeclaredElement NewDeclaredElement => myPointer.FindDeclaredElement();
        public override string NewName { get; }
        public override string OldName { get; }
        public override IDeclaredElement PrimaryDeclaredElement => myPointer.FindDeclaredElement();
        public override IList<IDeclaredElement> SecondaryDeclaredElements => null;

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
                        (string) nameParameter.ConstantValue.Value == nameArgument)
                    {
                        list.Add(attribute);
                    }
                }
            }

            return list;
        }

        private void RemoveFromTextualOccurrences(IRenameRefactoring executer, IFieldDeclaration fieldDeclaration)
        {
            if (!(executer.Workflow is RenameWorkflow workflow))
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

        [CanBeNull]
        private IAttribute CreateFormerlySerializedAsAttribute(IPsiModule module)
        {
            var elementFactory = CSharpElementFactory.GetInstance(module);
            var attributeType = TypeFactory.CreateTypeByCLRName(KnownTypes.FormerlySerializedAsAttribute, module);
            var attributeTypeElement = attributeType.GetTypeElement();
            if (attributeTypeElement == null)
                return null;

            return elementFactory.CreateAttribute(attributeTypeElement, new[]
                {
                    new AttributeValue(new ConstantValue(OldName, module))
                },
                EmptyArray<Pair<string, AttributeValue>>.Instance);
        }
    }
}