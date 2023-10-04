using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree
{
    [Language(typeof(UxmlLanguage))]
    public class UxmlElementTypes : XmlElementTypes
    {
        private const int BASE_INDEX = 1500;

        public UxmlElementTypes(XmlTokenTypes xmlTokenTypes) : base(xmlTokenTypes)
        {
            UXML_TAG = new UXML_TAG_TYPE(this, BASE_INDEX + 1);
            NAMESPACE_ALIAS = new NAMESPACE_ALIAS_TYPE(this, BASE_INDEX + 2);
        }

        public XmlCompositeNodeType UXML_TAG { get; }
        public XmlCompositeNodeType NAMESPACE_ALIAS { get; }

        public abstract class UxmlCompositeNodeType : XmlCompositeNodeType
        {
            protected UxmlCompositeNodeType(string s, XmlElementTypes types, int index)
                : base(s, types, index)
            {
            }

            public UxmlTokenTypes UxmlTokenTypes => (UxmlTokenTypes)XmlTokenTypes;
        }
        
        
        private class UXML_TAG_TYPE : UxmlCompositeNodeType
        {
            public UXML_TAG_TYPE(XmlElementTypes types, int index)
                : base("UXML_TAG", types, index) { }
            public override CompositeElement Create() { return new UxmlTagHeader(this); }
        }

        private class NAMESPACE_ALIAS_TYPE : UxmlCompositeNodeType
        {
            public NAMESPACE_ALIAS_TYPE(XmlElementTypes types, int index)
                : base("NAMESPACE_ALIAS", types, index)
            {
            }

            public override CompositeElement Create()
            {
                return new UxmlNamespaceAliasAttribute(this);
            }
        }
    }
}