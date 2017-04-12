using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    public abstract class StringLiteralReferenceFactoryBase : IReferenceFactory
    {
        public abstract ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences);

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            var literal = element as ILiteralExpression;
            if (literal != null && literal.ConstantValue.IsString())
                return names.Contains((string) literal.ConstantValue.Value);
            return false;
        }
    }
}