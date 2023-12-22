#nullable enable

using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree
{
    /// <summary><see cref="IDeclaration"/> which defines structure for features like Structure View or Breadcrumbs.</summary>
    public interface IStructuralDeclaration : IDeclaration
    {
        IStructuralDeclaration? ContainingDeclaration { get; }
        IEnumerable<IStructuralDeclaration> GetMemberDeclarations();
    }
}