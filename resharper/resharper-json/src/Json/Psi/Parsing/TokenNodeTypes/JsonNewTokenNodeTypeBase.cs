﻿using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodeTypes
{
    internal abstract class JsonNewTokenNodeTypeBase : TokenNodeType
    {
        protected JsonNewTokenNodeTypeBase(string s, int index)
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