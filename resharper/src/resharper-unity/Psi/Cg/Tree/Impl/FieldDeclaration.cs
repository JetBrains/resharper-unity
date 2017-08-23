using System.Xml;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.Impl
{
    internal partial class FieldDeclaration
    {
        public XmlNode GetXMLDoc(bool inherit)
        {
            throw new System.NotImplementedException();
        }

        public void SetName(string name)
        {
            throw new System.NotImplementedException();
        }

        public TreeTextRange GetNameRange()
        {
            throw new System.NotImplementedException();
        }

        public bool IsSynthetic()
        {
            throw new System.NotImplementedException();
        }

        public IDeclaredElement DeclaredElement { get; }
        public string DeclaredName { get; }
    }
}