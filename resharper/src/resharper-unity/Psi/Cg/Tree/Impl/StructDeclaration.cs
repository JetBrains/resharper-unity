using System.Xml;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.Impl
{
    internal partial class StructDeclaration
    {
        public XmlNode GetXMLDoc(bool inherit)
        {
            return null;
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
            return false; 
        }

        public IDeclaredElement DeclaredElement { get; }

        public string DeclaredName => NameNode.NameToken.GetText();
    }
}