#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabCommandBase : ShaderLabDeclarationBase, IShaderLabCommand
    {
        public IStructuralDeclaration? ContainingDeclaration => GetContainingNode<IStructuralDeclaration>();
        public virtual IEnumerable<IStructuralDeclaration> GetMemberDeclarations() => EmptyList<IStructuralDeclaration>.Enumerable;

        /// <summary>There a confusion between GetName which is for command name and a name assigned to entity defined with command. I.e. for <c>Shader "Foo"</c> Shader is a name and Foo is an entity name.</summary>
        public virtual ITokenNode? GetEntityNameToken() => null;  

        public sealed override string? GetName() => ((IShaderLabCommand)this).CommandKeyword?.GetText();

        public sealed override TreeTextRange GetNameRange() => ((IShaderLabCommand)this).CommandKeyword?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;

        protected override IDeclaredElement? TryCreateDeclaredElement()
        {
            if (GetSourceFile() is not { } sourceFile || GetName() is not { } name)
                return null;
            return new ShaderLabCommandDeclaredElement(name, GetEntityNameToken()?.GetText(), sourceFile, GetTreeStartOffset().Offset);
        }

        #region IShaderLabCommand
        /// <summary>Push <see cref="IShaderLabCommand"/> interface down by hierarchy. Can't just make it abstract, because generator doesn't support overriding of existing methods.</summary>
        ITokenNode IShaderLabCommand.CommandKeyword => throw new NotImplementedException("CommandKeyword should be implemented in derived class");
        #endregion
    }
}