using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    /// <summary>
    /// Parse the (filtered) pre-processor tokens
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pre-processor tokens can occur anywhere, which makes it harder to parse the rest of the file. So, like
    /// whitespace and comments, pre-processor tokens are marked as filtered, and don't get seen by the parser.
    /// However, we need to combine some of the tokens, (such as PP_WARNING and PP_MESSAGE) into elements. This
    /// pre-processor will loop over the original (not filtered) lexer and create and store the elements. They
    /// don't get inserted into the tree at this point, but are used by the missing tokens inserter.
    /// </para>
    /// <para>
    /// Note that the C# parser/preprocessor is a little more complex. The pre-processor tokens aren't filtered,
    /// and can actually contain comments and whitespace. So the C# pre-processor will run on the filtering lexer,
    /// and create and collect pre-processor elements. The missing tokens inserter has to be run on the newly
    /// created pre-processor elements to reinsert any comments. The collection of pre-processor elements are
    /// then fed back into the filtering lexer, so now the parser uses a lexer that removes whitespace, newlines,
    /// comments and pre-processor tokens, and the final missing tokens inserter will reinsert the created
    /// pre-processor elements.
    /// </para>
    /// </remarks>
    internal class ShaderLabPreProcessor
    {
        private readonly Dictionary<int, TreeElement> myPpDirectivesByOffset = new Dictionary<int, TreeElement>();

        public TreeElement GetPPDirectiveAtOffset(int offset)
        {
            TreeElement element;
            return myPpDirectivesByOffset.TryGetValue(offset, out element) ? element : null;
        }

        // The pre-processor tokens are filtered, so don't get parsed by the 
        public void Run(ILexer<int> lexer, ShaderLabParser parser, SeldomInterruptChecker interruptChecker)
        {
            for (var tokenType = lexer.TokenType; tokenType != null; tokenType = lexer.TokenType)
            {
                interruptChecker.CheckForInterrupt();

                if (tokenType == ShaderLabTokenType.PP_ERROR
                    || tokenType == ShaderLabTokenType.PP_WARNING
                    || tokenType == ShaderLabTokenType.PP_LINE)
                {
                    var startOffset = lexer.TokenStart;
                    var directive = parser.ParsePreprocessorDirective();
                    myPpDirectivesByOffset[startOffset] = directive;
                }
                else
                {
                    lexer.Advance();
                }
            }
        }
    }
}