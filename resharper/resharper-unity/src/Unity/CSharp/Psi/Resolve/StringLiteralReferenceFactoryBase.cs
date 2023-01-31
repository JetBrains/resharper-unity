#nullable enable

using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    // Note that this is only useful for languages whose literal expressions derive from ILiteralExpression.
    // I.e. C# and VB, not JavaScript!
    public abstract class StringLiteralReferenceFactoryBase : IReferenceFactory
    {
        public abstract ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences);

        public virtual bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            if (element is ILiteralExpression literal && literal.ConstantValue.IsNotNullString(out var literalText))
                return names.Contains(literalText);
            return false;
        }

        protected static ICSharpLiteralExpression? GetValidStringLiteralExpression(ITreeNode element)
        {
            var literal = element as ICSharpLiteralExpression;
            if (literal == null || !literal.ConstantValue.IsString())
                return null;

            return literal;
        }
    }
}
