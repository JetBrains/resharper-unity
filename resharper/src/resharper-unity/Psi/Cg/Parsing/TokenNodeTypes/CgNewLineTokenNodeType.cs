using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes
{
    internal class CgNewLineTokenNodeType : CgTokenNodeTypeBase
    {
        public CgNewLineTokenNodeType(int index)
            : base("NEW_LINE", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            throw new NotImplementedException();
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            throw new NotImplementedException();
        }

        public override bool IsWhitespace => true;
        public override string TokenRepresentation => @"\r\n";
    }
}