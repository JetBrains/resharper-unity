using JetBrains.Application.Components;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Xml.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi
{
    [Language(typeof(UxmlLanguage))]
    internal class UxmlLanguageService : XmlLanguageService
    {
        public UxmlLanguageService(XmlLanguage languageType, ILazy<IConstantValueService> constantValueService,
            ILazy<XmlTokenTypes> tokenTypes, ILazy<IXmlElementFactory> elementFactory, ILazy<IXmlCodeFormatter> codeFormatter,
            CommonIdentifierIntern commonIdentifierIntern) : base(languageType, constantValueService, tokenTypes,
            elementFactory, codeFormatter, commonIdentifierIntern)
        {
            
        }
        
        public override IParser CreateParser(
            ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
        {
            return new UxmlParser(lexer, ElementFactory, myCommonIdentifierIntern);
        }
    }

    internal class UxmlParser : IParser
    {
        private readonly ILexer Lexer;
        private readonly IXmlElementFactory ElementFactory;
        private readonly CommonIdentifierIntern myCommonIdentifierIntern;

        public UxmlParser(ILexer lexer, IXmlElementFactory elementFactory, CommonIdentifierIntern commonIdentifierIntern)
        {
            Lexer = lexer;
            ElementFactory = elementFactory;
            myCommonIdentifierIntern = commonIdentifierIntern;
        }

        public virtual IFile ParseFile()
        {
            using var intern = myCommonIdentifierIntern.GetOrCreateIntern();

            var builder = new XmlTreeBuilder(ElementFactory, DefaultXmlElementFactoryContext.Instance, intern);
            return builder.BuildXml(Lexer);
        }
    }
}