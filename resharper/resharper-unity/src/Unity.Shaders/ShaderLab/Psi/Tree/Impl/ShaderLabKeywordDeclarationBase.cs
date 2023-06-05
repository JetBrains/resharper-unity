#nullable enable

using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    /// <summary><see cref="ShaderLabKeywordDeclarationBase"/> is a base class for all ShaderLab keyword-based elements like commands and code blocks.</summary>
    public abstract class ShaderLabKeywordDeclarationBase : ShaderLabDeclarationBase
    {
        private readonly CachedPsiValueWithOffsets<IDeclaredElement> myCachedDeclaredElement = new();

        protected virtual string? TryGetDeclaredName() => (Keyword?.NodeType as ITokenNodeType)?.TokenRepresentation;
        
        protected abstract ITokenNode? Keyword { get; }
        protected abstract DeclaredElementType ElementType { get; }

        public sealed override IDeclaredElement? DeclaredElement => TryGetDeclaredName() is { } name ? 
            myCachedDeclaredElement.GetValue(this, name, static (self, name) => self.CreateDeclaredElement(name)) : 
            null;
        
        public sealed override string DeclaredName => TryGetDeclaredName() ?? SharedImplUtil.MISSING_DECLARATION_NAME; 
        public override void SetName(string name) => throw new NotSupportedException();
        public override TreeTextRange GetNameRange() => Keyword?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;

        private IDeclaredElement CreateDeclaredElement(string name) => new SimpleShaderLabDeclaredElement(name, GetSourceFile()!, GetTreeStartOffset().Offset, ElementType);
    }
}