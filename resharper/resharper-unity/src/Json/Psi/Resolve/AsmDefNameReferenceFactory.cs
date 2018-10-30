using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve
{
    public class AsmDefNameReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<AsmDefNameReference>(oldReferences, element))
                return oldReferences;

            if (element.IsReferencesStringLiteralValue())
            {
                return new ReferenceCollection(new AsmDefNameReference((IJavaScriptLiteralExpression) element));
            }

            return ReferenceCollection.Empty;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            if (element is IJavaScriptLiteralExpression literal && literal.IsStringLiteral())
                return names.Contains(literal.GetStringValue() ?? string.Empty);
            return false;
        }
    }
}