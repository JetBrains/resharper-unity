#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class PropertyDeclaration
    {
        public override string? GetName() => Name?.GetText();
        
        public override TreeTextRange GetNameRange() => Name?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;

        public override void SetName(string name)
        {
            using (WriteLockCookie.Create())
            {
                // TODO: Perhaps add SetName to ShaderLabIdentifier?
                var identifier = new ShaderLabIdentifier();
                identifier.AddChild(new Identifier(name));
                ModificationUtil.ReplaceChild(Name, identifier);
            }
        }

        protected override IDeclaredElement? TryCreateDeclaredElement() => GetSourceFile() is { } sourceFile ? new PropertyDeclaredElement(DeclaredName, sourceFile, GetTreeStartOffset().Offset) : null;
    }
}