using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes
{
    internal abstract class CgTokenNodeTypeBase : TokenNodeType
    {
        protected CgTokenNodeTypeBase(string s, int index)
            : base(s, index)
        {
        }

        public override bool IsWhitespace => false;
        public override bool IsComment => false;
        public override bool IsStringLiteral => false;
        public override bool IsConstantLiteral => false;
        public override bool IsIdentifier => false;
        public override bool IsKeyword => false;
    }
}