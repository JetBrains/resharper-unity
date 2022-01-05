using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public class SyncVarHookReferenceFactory : StringLiteralReferenceFactoryBase
    {
        public override ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<SyncVarHookReference>(oldReferences, element))
                return oldReferences;

            var literal = GetValidStringLiteralExpression(element);
            if (literal == null)
                return ReferenceCollection.Empty;

            var propertyAssignment = literal.GetContainingNode<IPropertyAssignment>();
            var propertyAssignmentReference = propertyAssignment?.Reference;
            if (propertyAssignmentReference == null || propertyAssignmentReference.GetName() != "hook")
            {
                return ReferenceCollection.Empty;
            }

            var assignedField = propertyAssignmentReference.Resolve().DeclaredElement as IField;
            var attributeType = assignedField?.GetContainingType();
            if (attributeType == null || !Equals(attributeType.GetClrName(), KnownTypes.SyncVarAttribute))
                return ReferenceCollection.Empty;

            var multipleFieldDeclaration = propertyAssignment.GetContainingNode<IMultipleFieldDeclaration>();
            var declaredFieldTypeUsage = multipleFieldDeclaration?.TypeUsage;
            var containingType = multipleFieldDeclaration?.GetContainingNode<IClassLikeDeclaration>()?.DeclaredElement;
            if (containingType != null && declaredFieldTypeUsage != null)
            {
                var declaredFieldType = CSharpTypeFactory.CreateType(declaredFieldTypeUsage);
                var reference = new SyncVarHookReference(containingType, declaredFieldType, literal);
                return new ReferenceCollection(reference);
            }

            return ReferenceCollection.Empty;
        }
    }
}