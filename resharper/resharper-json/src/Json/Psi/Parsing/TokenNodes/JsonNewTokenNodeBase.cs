using System.Text;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Parsing.TokenNodes
{
    public abstract class JsonNewTokenNodeBase : LeafElementBase, ITokenNode, IJsonNewTreeNode
    {
        public override PsiLanguageType Language => LanguageFromParent;

        public TokenNodeType GetTokenType()
        {
            return (TokenNodeType) NodeType;
        }

        public override string ToString()
        {
            return $"{base.ToString()}(type:{NodeType}, text:{GetText()})";
        }

        public override StringBuilder GetText(StringBuilder to)
        {
            to.Append(GetText());
            return to;
        }

        public override IBuffer GetTextAsBuffer()
        {
            return new StringBuffer(GetText());
        }

        public void Accept(TreeNodeVisitor visitor)
        {
            visitor.VisitNode(this);
        }

        public void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context)
        {
            visitor.VisitNode(this, context);
        }

        public TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context)
        {
            return visitor.VisitNode(this, context);
        }
    }
}