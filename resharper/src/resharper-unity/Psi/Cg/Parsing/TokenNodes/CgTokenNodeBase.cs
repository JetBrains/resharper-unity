using System.Text;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodes
{
    public abstract class CgTokenNodeBase : LeafElementBase, ITokenNode
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
    }
}