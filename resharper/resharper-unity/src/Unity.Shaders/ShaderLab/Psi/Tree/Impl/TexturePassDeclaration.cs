#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class TexturePassDeclaration
    {
        private ITokenNode? GetNameToken() => Command?.GetEntityNameToken(); 
        
        public override string? GetName() => GetNameToken()?.GetUnquotedText();

        public override TreeTextRange GetNameRange() => GetNameToken()?.GetUnquotedTreeTextRange() ?? TreeTextRange.InvalidRange;

        protected override IDeclaredElement? TryCreateDeclaredElement() => GetSourceFile() is { } sourceFile && GetName() is { Length: > 0 } name 
            ? new TexturePassDeclaredElement(name.ToUpper(), sourceFile, GetTreeStartOffset().Offset) // Unity always converts pass name to uppercase 
            : null;

        public override void SetName(string name) => this.SetStringLiteral(GetNameToken(), name);
    }
}