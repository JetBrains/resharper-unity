using JetBrains.Annotations;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.TreeBuilder;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing
{
    public class JsonNewTreeBuilder : TreeStructureBuilderBase, IPsiBuilderTokenFactory
    {
        
        private readonly PsiBuilder myBuilder;
        
        public JsonNewTreeBuilder(ILexer<int> lexer, Lifetime lifetime) : base(lifetime)
        {
            myBuilder = new PsiBuilder(lexer, ElementType.JSON_NEW_FILE, this, lifetime);
        }

        protected override string GetExpectedMessage(string name)
        {
            return ParserMessages.GetExpectedMessage(name);
        }

        protected override PsiBuilder Builder => myBuilder;
        protected override TokenNodeType NewLine => JsonNewTokenNodeTypes.NEW_LINE;
        protected override NodeTypeSet CommentsOrWhiteSpacesTokens => JsonNewTokenNodeTypes.COMMENTS_AND_WHITESPACES;
     
        public LeafElementBase CreateToken(TokenNodeType tokenNodeType, IBuffer buffer, int startOffset, int endOffset)
        {
            return tokenNodeType.Create(buffer, new TreeOffset(startOffset), new TreeOffset(endOffset));
        }
        
        public void ParseFile()
        {
            var mark = MarkNoSkipWhitespace();
            ParseJsonValue();
            SkipWhitespaces();
            
            if (!myBuilder.Eof())
            {
                var errorMark = MarkNoSkipWhitespace();
                while (!myBuilder.Eof())
                {
                    Advance();
                }

                myBuilder.Error(errorMark, GetExpectedMessage("EOF"));
            }
            
            Done(mark, ElementType.JSON_NEW_FILE);
        }

        public bool ParseJsonValue()
        {
            var mark = Mark();
            
            var tt = myBuilder.GetTokenType();
            if (tt == JsonNewTokenNodeTypes.LBRACE)
            {
                return ParseJsonObject(mark);
            } else if (tt == JsonNewTokenNodeTypes.LBRACKET)
            {
                return ParseJsonArray(mark);
            }
            else
            {
                return ParseJsonLiteral(mark);
            }
        }

        private bool ParseJsonObject(int mark)
        {
            if (!ExpectToken(JsonNewTokenNodeTypes.LBRACE))
            {
                myBuilder.Drop(mark);
                return false;
            }
            
            if (GetTokenType() == JsonNewTokenNodeTypes.RBRACE)
            {
                Advance();
                Done(mark, ElementType.JSON_NEW_OBJECT); 
                return true;
            }
            
            ParseMembers();

            if (!ExpectToken(JsonNewTokenNodeTypes.RBRACE))
            {
                myBuilder.Drop(mark);
                return false;
            }
            Done(mark, ElementType.JSON_NEW_OBJECT);
            return true;
        }

        private void ParseMembers()
        {
            ParseMember();
            while (GetTokenType() == JsonNewTokenNodeTypes.COMMA)
            {
                Advance();
                ParseMember();
            }
        }
        
        private bool ParseMember()
        {
            var mark = Mark();
            if (!ExpectToken(JsonNewTokenNodeTypes.DOUBLE_QUOTED_STRING))
            {
                myBuilder.Drop(mark);
                return false;
            }
            
            if (!ExpectToken(JsonNewTokenNodeTypes.COLON))
            {
                myBuilder.Drop(mark);
                return false;
            }
            
            ParseJsonValue();
            
            Done(mark,ElementType.JSON_NEW_MEMBER);
            return true;
        }

        private bool ParseJsonArray(int mark)
        {
            if (!ExpectToken(JsonNewTokenNodeTypes.LBRACKET))
            {
                myBuilder.Drop(mark);
                return false;
            }

            if (GetTokenType() == JsonNewTokenNodeTypes.RBRACKET)
            {
                Advance();
                Done(mark, ElementType.JSON_NEW_ARRAY); 
                return true;
            }
            
            ParseJsonValue();

            while (GetTokenType() == JsonNewTokenNodeTypes.COMMA)
            {
                Advance();
                ParseJsonValue();
            }

            if (!ExpectToken(JsonNewTokenNodeTypes.RBRACKET))
            {
                myBuilder.Drop(mark);
                return false;
            }

            Done(mark, ElementType.JSON_NEW_ARRAY);
            return true;
        }

        public void ParseJsonLiteralExpression()
        {
            var mark = Mark();
            ParseJsonLiteral(mark);
        }
        
        private bool ParseJsonLiteral(int mark)
        {
            if (GetTokenType() == JsonNewTokenNodeTypes.DOUBLE_QUOTED_STRING ||
                GetTokenType() == JsonNewTokenNodeTypes.NUMERIC_LITERAL ||
                GetTokenType() == JsonNewTokenNodeTypes.TRUE_KEYWORD ||
                GetTokenType() == JsonNewTokenNodeTypes.FALSE_KEYWORD ||
                GetTokenType() == JsonNewTokenNodeTypes.NULL_KEYWORD)
            {
                Advance();
                Done(mark, ElementType.JSON_NEW_LITERAL_EXPRESSION);
                return true;
            }

            if (GetTokenType() == JsonNewTokenNodeTypes.SINGLE_QUOTED_STRING)
            {
                Advance();
                myBuilder.Error(mark, GetExpectedMessage("Double quoted string"));
                return true;
            }
            
            myBuilder.Drop(mark);

            return false;
        }

        [MustUseReturnValue]
        private int MarkNoSkipWhitespace()
        {
            // this.Mark() calls SkipWhitespace() first
            return myBuilder.Mark();
        }
        
    }
}