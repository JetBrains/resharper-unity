using JetBrains.ReSharper.Psi.Impl.Shared.References;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Xml.Tree.References;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    public interface IUxmlNamespaceReference : IReferenceWithinElement, IXmlCompletableReference, IQualifier
    {
    }
}