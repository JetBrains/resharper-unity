using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Gen;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    internal class ShaderLabParser : ShaderLabParserGenerated, IShaderLabParser
    {
        [NotNull]
        private readonly ILexer<int> myOriginalLexer;
        private readonly CommonIdentifierIntern myIntern;
        private readonly ShaderLabPreProcessor myPreProcessor;
        private ITokenIntern myTokenIntern;

        public ShaderLabParser([NotNull] ILexer<int> lexer, CommonIdentifierIntern intern)
        {
            myOriginalLexer = lexer;
            myIntern = intern;

            // Create the pre-processor elements, using the unfiltered lexer, so we'll see the
            // preprocessor tokens!
            SetLexer(myOriginalLexer);
            myPreProcessor = new ShaderLabPreProcessor();
            myPreProcessor.Run(myOriginalLexer, this, new SeldomInterruptChecker());

            // Reset the lexer to the beginning, and use the filtered lexer. Pass in the
            // preprocessor, so we can filter on CG_CONTENT and CG_END when they're used
            // for include blocks (they're not filtered tokens normally)
            SetLexer(new ShaderLabFilteringLexer(lexer, myPreProcessor));
        }

        public ITokenIntern TokenIntern => myTokenIntern ?? (myTokenIntern = new LexerTokenIntern(10));

        public IFile ParseFile()
        {
            return myIntern.DoWithIdentifierIntern(intern =>
            {
                var element = ParseShaderLabFile();
                InsertMissingTokens(element, intern);
                return (IFile) element;
            });
        }

        IVectorLiteral IShaderLabParser.ParseVectorLiteral()
        {
            return myIntern.DoWithIdentifierIntern(intern =>
            {
                var element = ParseVectorLiteral();
                InsertMissingTokens(element, intern);
                return (IVectorLiteral) element;
            });
        }

        protected override TreeElement CreateToken()
        {
            var tokenType = myLexer.TokenType;

            Assertion.Assert(tokenType != null, "tokenType != null");

            // Node offsets aren't stored during parsing. However, we need the absolute file
            // offset position so we can re-insert filtered tokens, so call SetOffset here.
            // Implementation details: This is non-obvious, so I'm going into implementation
            // details. SetOffset updates TreeElement.myCachedOffsetData to an absolute offset,
            // indicated by having a negative value, offset by 2. In other words, -1 means unset,
            // -2 means 0 and -3 means an absolute offset of 1. This offset is only valid during
            // parsing! After parsing, TreeElement.myCachedOffsetData is re-used to cache the offset
            // (relative to the parent node) calculated from a call to GetTextLength or GetTreeStartOffset
            // and invalidated by SubTreeChanged, or otherwise modifying the tree. The relative
            // offset is indicated by a positive value, or 0. The implementation of
            // MissingTokenInserterBase.GetLeafOffset has a minor optimisation that tries to downcast
            // the leaf element to BindedToBufferLeafElement, and uses the Offset property there.
            // If all tokens inherit from BindedToBufferLeafElement, the offset is known at parse
            // time, and we don't need to call SetOffset here (doing so is ignored)
            var tokenStart = myLexer.TokenStart;
            var element = CreateToken(tokenType);
            if (element is LeafElementBase leaf)
                SetOffset(leaf, tokenStart);
            return element;
        }

        public override TreeElement ParseErrorElement()
        {
            // NOTE: Doesn't Advance
            var result = TreeElementFactory.CreateErrorElement(ParserMessages.GetUnexpectedTokenMessage());
            return result;
        }

        public override TreeElement ParseErrorElementWithoutRBrace()
        {
            return ParseErrorElement();
        }

        private bool IsIdentifier(TokenNodeType token)
        {
            // TODO: This should just be a bitset
            // At the very least, it needs to be a better check
            return token == ShaderLabTokenType.IDENTIFIER || token.IsKeyword;
        }

        public override TreeElement ParseErrorPropertyValue()
        {
            var result = TreeElementFactory.CreateErrorElement(ParserMessages.GetUnexpectedTokenMessage());

            // Consume until we hit either an identifier (start of another property)
            // an LBRACK (start of attributes for another property) or RBRACE
            while (!IsIdentifier(myOriginalLexer.TokenType)
                && myOriginalLexer.TokenType != ShaderLabTokenType.LBRACK
                && myOriginalLexer.TokenType != ShaderLabTokenType.RBRACE)
            {
                if (myOriginalLexer.TokenType == ShaderLabTokenType.LBRACE)
                    SkipNestedBraces(result);
                else
                    Skip(result);
            }
            return result;
        }

        private void SkipNestedBraces(CompositeElement result)
        {
            while (myOriginalLexer.TokenType != ShaderLabTokenType.RBRACE)
            {
                Skip(result);
                if (myOriginalLexer.TokenType == ShaderLabTokenType.LBRACE)
                    SkipNestedBraces(result);
            }

            // Skip the final RBRACE
            Skip(result);
        }

        public override void ParseErrorTexturePropertyBlockValues(CompositeElement result)
        {
            // Parse anything with the `{ }` of a texture property block
            if (myOriginalLexer.TokenType == ShaderLabTokenType.RBRACE)
                return;

            var errorElement = TreeElementFactory.CreateErrorElement(ParserMessages.GetUnexpectedTokenMessage());
            while (myOriginalLexer.TokenType != ShaderLabTokenType.RBRACE)
                Skip(errorElement);
            result.AppendNewChild(errorElement);
        }

        private void InsertMissingTokens(TreeElement root, ITokenIntern intern)
        {
            var interruptChecker = new SeldomInterruptChecker();
            ShaderLabMissingTokensInserter.Run(root, myOriginalLexer, this, myPreProcessor, interruptChecker, intern);
        }

        private TreeElement CreateToken(TokenNodeType tokenType)
        {
            Assertion.Assert(tokenType != null, "tokenType != null");

            LeafElementBase element;
            if (tokenType == ShaderLabTokenType.NUMERIC_LITERAL
                || tokenType == ShaderLabTokenType.STRING_LITERAL
                || tokenType == ShaderLabTokenType.IDENTIFIER
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
    }
}