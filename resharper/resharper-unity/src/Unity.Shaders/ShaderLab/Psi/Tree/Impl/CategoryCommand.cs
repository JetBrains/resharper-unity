#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Services.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class CategoryCommand
    {
        public override IEnumerable<IStructuralDeclaration> GetMemberDeclarations()
        {
            if (Value is not ICategoryValue categoryValue)
                yield break;
            foreach (var stateCommand in categoryValue.StateCommands)
            {
                if (stateCommand is IStructuralDeclaration declaration)
                    yield return declaration;
            }
            foreach (var shaderBlock in categoryValue.ShaderBlocks)
            {
                if (shaderBlock.FirstChild is IStructuralDeclaration shaderBlockCommand)
                    yield return shaderBlockCommand;
            }
        }

        protected override string TryGetDeclaredName()
        {
            var nameToken = (Value as ICategoryValue)?.StateCommandsEnumerable.LastOrDefaultOfType<IStateCommand, INameCommand>()?.Name;
            return ShaderLabTreeHelpers.FormatCommandDeclaredName(ShaderLabTokenType.CATEGORY_KEYWORD, nameToken);
        }
    }
}