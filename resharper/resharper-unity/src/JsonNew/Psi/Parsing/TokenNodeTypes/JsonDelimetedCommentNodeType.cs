﻿using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing.TokenNodeTypes
{
    internal class JsonNewDelimetedCommentNodeType : JsonNewTokenNodeTypeBase
    {
        public JsonNewDelimetedCommentNodeType(int index)
            : base("DELIMITED_COMMENT", index)
        {
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new JsonNewDelimitedCommentNode(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override LeafElementBase Create(string token)
        {
            return new JsonNewDelimitedCommentNode(token);
        }

        public override bool IsComment => true;
        public override string TokenRepresentation => "/* comment */";
    }
}