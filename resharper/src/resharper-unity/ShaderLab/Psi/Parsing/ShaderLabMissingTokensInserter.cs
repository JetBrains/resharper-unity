using System.Text;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    internal class ShaderLabMissingTokensInserter : MissingTokenInserterBase
    {
        private readonly ILexer myLexer;
        private readonly ShaderLabPreProcessor myPreProcessor;

        private ShaderLabMissingTokensInserter(ILexer lexer, ITokenOffsetProvider offsetProvider, ShaderLabPreProcessor preProcessor, SeldomInterruptChecker interruptChecker, ITokenIntern intern)
            : base(offsetProvider, interruptChecker, intern)
        {
            myLexer = lexer;
            myPreProcessor = preProcessor;
        }

        protected override void ProcessLeafElement(TreeElement leafElement)
        {
            var leafOffset = GetLeafOffset(leafElement).Offset;

            if (myLexer.TokenType != null && myLexer.TokenStart < leafOffset)
            {
                var anchor = leafElement;
                var parent = anchor.parent;
                while (anchor == parent.FirstChild && parent.parent != null)
                {
                    anchor = parent;
                    parent = parent.parent;
                }

                while (myLexer.TokenType != null && myLexer.TokenStart < leafOffset)
                {
                    var token = CreateMissingToken();

                    parent.AddChildBefore(token, anchor);

                    var skipTo = myLexer.TokenStart + token.GetTextLength();
                    while (myLexer.TokenType != null && myLexer.TokenStart < skipTo)
                        myLexer.Advance();
                }
            }

            var leafEndOffset = leafOffset + leafElement.GetTextLength();
            while (myLexer.TokenType != null && myLexer.TokenStart < leafEndOffset)
                myLexer.Advance();
        }

        private TreeElement CreateMissingToken()
        {
            var ppDirective = myPreProcessor.GetPPDirectiveAtOffset(myLexer.TokenStart);
            if (ppDirective != null)
                return ppDirective;

            var tokenType = myLexer.TokenType;
            if (tokenType == ShaderLabTokenType.WHITESPACE)
                return new Whitespace(myWhitespaceIntern.Intern(myLexer));

            if (tokenType == ShaderLabTokenType.NEW_LINE)
                return new NewLine(myWhitespaceIntern.Intern(myLexer));

            return TreeElementFactory.CreateLeafElement(myLexer);
        }

        public static void Run(TreeElement node, ILexer lexer, ITokenOffsetProvider offsetProvider, ShaderLabPreProcessor preProcessor, SeldomInterruptChecker interruptChecker, ITokenIntern intern)
        {
            Assertion.Assert(node.parent == null, "node.parent == null");

            var root = node as CompositeElement;
            if (root == null)
                return;

            // Append an EOF token so we insert filtered tokens right up to
            // the end of the file
            var eof = new EofToken(lexer.Buffer.Length);
            root.AppendNewChild(eof);

            var inserter = new ShaderLabMissingTokensInserter(lexer, offsetProvider, preProcessor, interruptChecker, intern);

            // Reset the lexer, walk the tree and call ProcessLeafElement on each leaf element
            lexer.Start();
            inserter.Run(root);

            root.DeleteChildRange(eof, eof);
        }

        private class EofToken : LeafElementBaseWithCustomOffset
        {
            public EofToken(int position)
                : base(new TreeOffset(position))
            {
            }

            public override int GetTextLength() => 0;
            public override StringBuilder GetText(StringBuilder to) => to;
            public override IBuffer GetTextAsBuffer() => new StringBuffer(string.Empty);

            public override string GetText() => string.Empty;
            public override NodeType NodeType => ShaderLabTokenType.EOF;
            public override PsiLanguageType Language => ShaderLabLanguage.Instance;
        }
    }
}