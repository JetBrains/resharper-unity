using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Xml.Parsing;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Parsing
{
    [Language(typeof (UxmlLanguage))]
    public class UxmlTreeNodeFactory : XmlTreeNodeFactory
    {
        [NotNull] private readonly UxmlElementTypes myElementType;
        public UxmlTreeNodeFactory([NotNull] UxmlLanguage languageType, [NotNull] XmlTokenTypes tokenTypes,
            [NotNull] UxmlElementTypes elementTypes) : base(languageType, tokenTypes, elementTypes)
        {
            myElementType = elementTypes;
        }

        public override IXmlAttribute CreateAttribute(
            IXmlIdentifier nameIdentifier, IXmlAttributeContainer attributeContainer, IXmlTagContainer parentTag,
            IXmlElementFactoryContext context)
        {
            // namespace alias
            if (nameIdentifier.XmlNamespace == "xmlns" || nameIdentifier.GetText() == "xmlns")
                return (IXmlAttribute)myElementType.NAMESPACE_ALIAS.Create();

            return base.CreateAttribute(nameIdentifier, attributeContainer, parentTag, context);
        }
    }
}