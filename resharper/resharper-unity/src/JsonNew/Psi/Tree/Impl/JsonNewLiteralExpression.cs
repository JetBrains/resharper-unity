using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree.Impl
{
    internal partial class JsonNewLiteralExpression
    {
        public ConstantValueTypes ConstantValueType
        {
            get
            {
                var token = Literal;
                if (token == null) return ConstantValueTypes.Unknown;

                var tokenType = token.GetTokenType();
                if (tokenType == JsonNewTokenNodeTypes.NULL_KEYWORD) return ConstantValueTypes.Null;
                if (tokenType == JsonNewTokenNodeTypes.TRUE_KEYWORD) return ConstantValueTypes.True;
                if (tokenType == JsonNewTokenNodeTypes.FALSE_KEYWORD) return ConstantValueTypes.False;
                if (tokenType == JsonNewTokenNodeTypes.NUMERIC_LITERAL) return ConstantValueTypes.Numeric;
                if (tokenType == JsonNewTokenNodeTypes.DOUBLE_QUOTED_STRING) return ConstantValueTypes.String;

                return ConstantValueTypes.Unknown;
            }
        }

        public TreeTextRange GetInnerTreeTextRange()
        {
            var token = Literal;
            if (token == null) return TreeTextRange.InvalidRange;

            var tokenType = token.GetTokenType();
            if (tokenType == JsonNewTokenNodeTypes.NULL_KEYWORD) return this.GetTreeTextRange();
            if (tokenType == JsonNewTokenNodeTypes.TRUE_KEYWORD) return this.GetTreeTextRange();
            if (tokenType == JsonNewTokenNodeTypes.FALSE_KEYWORD) return this.GetTreeTextRange();
            if (tokenType == JsonNewTokenNodeTypes.NUMERIC_LITERAL) return this.GetTreeTextRange();
            if (tokenType == JsonNewTokenNodeTypes.DOUBLE_QUOTED_STRING)
            {
                var text = token.GetText();
                if (text.Length <= 1) return TreeTextRange.InvalidRange;

                char firstChar = text[0],
                    lastChar = text[text.Length - 1];

                var treeTextRange = this.GetTreeTextRange();

                bool hasStartQuote = firstChar == '\"' || firstChar == '\'';
                bool hasEndQuote = hasStartQuote ? firstChar == lastChar : lastChar == '\"' || lastChar == '\'';

                if (!treeTextRange.IsValid() || !hasStartQuote && !hasEndQuote) return treeTextRange;

                var length = treeTextRange.Length;
                if (length < 1) return treeTextRange;

                if (!hasStartQuote) return treeTextRange.TrimRight(1);
                if (length >= 2 && hasEndQuote) return treeTextRange.TrimLeft(1).TrimRight(1);
                return treeTextRange.TrimLeft(1);
            }

            return TreeTextRange.InvalidRange;
        }

        public string GetStringValue()
        {
            var token = Literal;
            if (token == null) return null;

            var tokenType = token.GetTokenType();
            if (tokenType == JsonNewTokenNodeTypes.NULL_KEYWORD) return "null";
            if (tokenType == JsonNewTokenNodeTypes.TRUE_KEYWORD) return "true";
            if (tokenType == JsonNewTokenNodeTypes.FALSE_KEYWORD) return "false";
            if (tokenType == JsonNewTokenNodeTypes.NUMERIC_LITERAL) return token.GetText();
            if (tokenType == JsonNewTokenNodeTypes.DOUBLE_QUOTED_STRING)
            {
                return StringLiteralUtil.GetDoubleQuotedStringValue(token);
            }


            return null;
        }
    }
}