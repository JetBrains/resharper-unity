using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabProgramBase : ShaderLabCodeBlockBase
    {
        protected sealed override DeclaredElementType ElementType => ShaderLabDeclaredElementType.ProgramBlock;
    }
}