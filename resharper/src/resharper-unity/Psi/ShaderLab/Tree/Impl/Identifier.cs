using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree.Impl
{
    // A named identifier, so presumably only used when referring to a thing
    // e.g. property definition name, property reference name, shader name, etc.
    // Except shader name is a string literal...
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

    internal abstract partial class ShaderLabIdentifierStub
    {
        public abstract string Name { get; }
    }

    internal class ShaderLabIdentifier : ShaderLabIdentifierStub
    {
        public override string Name => GetText();
    }
}