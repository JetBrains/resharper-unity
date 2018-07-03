using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    // Note that this is only useful for languages whose literal expressions derive from ILiteralExpression.
    // I.e. C# and VB, not JavaScript!
    // TODO: I think it's time to create a CSharp top level folder
    public abstract class StringLiteralReferenceFactoryBase : IReferenceFactory
    {
        public abstract ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences);

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            if (element is ILiteralExpression literal && literal.ConstantValue.IsString())
                return names.Contains((string) literal.ConstantValue.Value);
            return false;
        }
    }
}