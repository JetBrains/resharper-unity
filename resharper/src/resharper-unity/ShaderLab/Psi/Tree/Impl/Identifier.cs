using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl
{
    // A token to represent an identifier name. Note that an identifier can also be another token
    // as the tokens for command names and values are not keywords as such
    internal class Identifier : ShaderLabTokenBase, IIdentifier
    {
        private readonly string myText;

        public Identifier(string text)
        {
            myText = text;
        }

        public override int GetTextLength() => myText.Length;
        public override string GetText() => myText;
        public override NodeType NodeType => ShaderLabTokenType.IDENTIFIER;

        // TODO: This doesn't seem right
        public string Name => GetText();
    }

    // An element to represent an identifier. We need an element instead of just a tree
    // node, because the name of the identifier might clash with one of the "keyword" tokens
    internal abstract partial class ShaderLabIdentifierStub
    {
        public abstract string Name { get; }
    }

    internal class ShaderLabIdentifier : ShaderLabIdentifierStub
    {
        public override string Name => GetText();
    }
}