using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Gen;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing
{
    internal class CgParser : CgParserGenerated, IParser
    {
        [NotNull]
        private readonly ILexer<int> myOriginalLexer;
        
        private readonly CommonIdentifierIntern myIntern;

        private ITokenIntern myTokenIntern;
        private CgPreProcessor myPreProcessor;

        private ITokenIntern TokenIntern => myTokenIntern ?? (myTokenIntern = new LexerTokenIntern(10));
        
        
        public CgParser([NotNull] ILexer<int> originalLexer, CommonIdentifierIntern intern)
        {
            myOriginalLexer = originalLexer;
            myIntern = intern;
            
            SetLexer(myOriginalLexer);
            
            myPreProcessor = new CgPreProcessor();
            myPreProcessor.Run(myOriginalLexer, this, new SeldomInterruptChecker());
            
            SetLexer(new CgFilteringLexer(myOriginalLexer, myPreProcessor));
        }
        
        protected override TreeElement CreateToken()
        {
            var tokenType = myLexer.TokenType;

            Assertion.Assert(tokenType != null, "tokenType != null");
            
            var tokenStart = myLexer.TokenStart;
            var element = CreateToken(tokenType);
            var leaf = element as LeafElementBase;
            if (leaf != null)
                SetOffset(leaf, tokenStart);
            
            return element;
        }

        public IFile ParseFile()
        {
            return myIntern.DoWithIdentifierIntern(intern =>
            {
                var element = ParseCgFile();
                InsertMissingTokens(element, intern);
                return (IFile) element;
            });
        }
        
        private void InsertMissingTokens(TreeElement root, ITokenIntern intern)
        {
            var interruptChecker = new SeldomInterruptChecker();
            CgMissingTokensInserter.Run(root, myOriginalLexer, this, myPreProcessor, interruptChecker, intern);
        }

        private TreeElement CreateToken(TokenNodeType tokenType)
        {
            Assertion.Assert(tokenType != null, "tokenType != null");

            LeafElementBase element;
            if (tokenType == CgTokenNodeTypes.IDENTIFIER
                || tokenType.IsKeyword)
            {
                // Interning the token text will allow us to reuse existing string instances.
                // The IEqualityComparer implementation will generate a hash code and compare
                // the current token text by looking directly into the lexer buffer, and does
                // not allocate a new string from the lexer, unless the string isn't already
                // interned.
                var text = TokenIntern.Intern(myLexer);
                element = tokenType.Create(text);
            }
            else
            {
                element = tokenType.Create(myLexer.Buffer,
                    new TreeOffset(myLexer.TokenStart),
                    new TreeOffset(myLexer.TokenEnd));
            }

            myLexer.Advance();

            return element;
        }

        public override TreeElement ParseErrorElement()
        {
            // doesn't advance
            var result = TreeElementFactory.CreateErrorElement(ParserMessages.GetUnexpectedTokenMessage());
            return result;
        }
    }
}