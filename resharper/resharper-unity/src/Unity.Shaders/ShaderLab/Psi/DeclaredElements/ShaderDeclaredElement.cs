#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public class ShaderDeclaredElement : ShaderLabDeclaredElementBase
    {
        public override DeclaredElementType GetElementType() => ShaderLabDeclaredElementType.Shader;
        
        public ShaderDeclaredElement(string shortName, IPsiSourceFile sourceFile, int treeOffset) : base(shortName, sourceFile, treeOffset) { }

        public override IList<IDeclaration> GetDeclarations() => SourceFile.GetPrimaryPsiFile() is ShaderLabFile file ? FixedList.ListOf<IDeclaration>(file) : EmptyList<IDeclaration>.Instance;
    }
}