#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Services.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabBlockCommandBase : ShaderLabCommandBase, IBlockCommand
    {
        public override IEnumerable<IStructuralDeclaration> GetMemberDeclarations() => ((IBlockCommand)this).Value?.Children<IStructuralDeclaration>() ?? EmptyList<IStructuralDeclaration>.Enumerable;

        #region IBlockCommand ahead declaration
        IBlockValue IBlockCommand.Value => throw new NotImplementedException();
        IBlockValue IBlockCommand.SetValue(IBlockValue param) => throw new NotImplementedException();
        #endregion
    }
}