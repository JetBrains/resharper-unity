using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class BindChannelsCommand
    {
        public override IEnumerable<IStructuralDeclaration> GetMemberDeclarations()
        {
            if (Value is not IBindChannelsValue bindChannelsValue)
                return EmptyList<IStructuralDeclaration>.Enumerable;
            return bindChannelsValue.BindCommandEnumerable.OfType<IStructuralDeclaration>();
        }
    }
}