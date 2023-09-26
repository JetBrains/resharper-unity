using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    public class UxmlReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            return oldReferences;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            return true;
        }
    }
}