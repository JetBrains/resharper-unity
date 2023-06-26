#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public class TexturePassDeclaredElement : ShaderLabDeclaredElementBase
    {
        public TexturePassDeclaredElement(string shortName, IPsiSourceFile sourceFile, int treeOffset) : base(shortName, sourceFile, treeOffset)
        {
        }

        public override DeclaredElementType GetElementType() => ShaderLabDeclaredElementType.TexturePass;
        
        public override IList<IDeclaration> GetDeclarations()
        {
            if (SourceFile.GetPrimaryPsiFile() is not IShaderLabFile psi)
                return EmptyList<IDeclaration>.InstanceList;

            var node = psi.FindNodeAt(TreeTextRange.FromLength(new TreeOffset(TreeOffset), 1));
            var declaration = node?.GetContainingNode<ITexturePassDeclaration>();
            return declaration != null ? FixedList.ListOf<IDeclaration>(declaration) : EmptyList<IDeclaration>.Instance;
        }
    }
}