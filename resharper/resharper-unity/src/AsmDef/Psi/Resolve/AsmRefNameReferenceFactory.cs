using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve
{
    public class AsmRefNameReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<AsmDefNameReference>(oldReferences, element))
                return oldReferences;

            return element.IsReferencePropertyValue()
                ? new ReferenceCollection(new AsmDefNameReference((IJsonNewLiteralExpression)element))
                : ReferenceCollection.Empty;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            if (element is IJsonNewLiteralExpression literal && literal.ConstantValueType == ConstantValueTypes.String)
                return names.Contains(literal.GetStringValue() ?? string.Empty);
            return false;
        }
    }
}