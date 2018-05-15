using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve
{
    public class AsmDefNameDummyReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<AsmDefNameDummyReference>(oldReferences, element))
                return oldReferences;

            return element.IsNameStringLiteralValue()
                ? new ReferenceCollection(new AsmDefNameDummyReference((IJavaScriptLiteralExpression) element))
                : ReferenceCollection.Empty;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            if (element is IJavaScriptLiteralExpression literal && literal.IsStringLiteral())
                return names.Contains(literal.GetStringValue() ?? string.Empty);
            return false;
        }
    }
}