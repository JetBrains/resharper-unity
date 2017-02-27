using JetBrains.Application;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree.Impl;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public class ShaderLabMissingTokensInserter : MissingTokenInserterBase
    {
        private readonly ILexer myLexer;

        private ShaderLabMissingTokensInserter(ILexer lexer, ITokenOffsetProvider offsetProvider,
            SeldomInterruptChecker interruptChecker, ITokenIntern intern)
            : base(offsetProvider, interruptChecker, intern)
        {
            myLexer = lexer;
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
            var tokenType = myLexer.TokenType;
            if (tokenType == ShaderLabTokenType.WHITESPACE)
                return new Whitespace(myWhitespaceIntern.Intern(myLexer));

            if (tokenType == ShaderLabTokenType.NEW_LINE)
                return new NewLine(myWhitespaceIntern.Intern(myLexer));

            return TreeElementFactory.CreateLeafElement(myLexer);
        }

        public static void Run(TreeElement node, ILexer lexer, ITokenOffsetProvider offsetProvider, SeldomInterruptChecker interruptChecker, ITokenIntern intern)
        {
            Assertion.Assert(node.parent == null, "node.parent == null");

            var root = node as CompositeElement;
            if (root == null)
                return;

            var inserter = new ShaderLabMissingTokensInserter(lexer, offsetProvider, interruptChecker, intern);

            // Reset the lexer, walk the tree and call ProcessLeafElement on each leaf element
            lexer.Start();
            inserter.Run(root);

        }
    }
}