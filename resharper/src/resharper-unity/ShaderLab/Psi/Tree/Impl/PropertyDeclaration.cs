using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl
{
    internal partial class PropertyDeclaration
    {
        private readonly CachedPsiValueWithOffsets<IDeclaredElement> myCachedDeclaredElement =
            new CachedPsiValueWithOffsets<IDeclaredElement>();

        public override IDeclaredElement DeclaredElement
        {
            get
            {
                if (Name == null)
                    return null;
                return myCachedDeclaredElement.GetValue(this,
                    () => CreateDeclaration(DeclaredName));
            }
        }

        public override string DeclaredName => Name?.GetText() ?? SharedImplUtil.MISSING_DECLARATION_NAME;

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

        public override TreeTextRange GetNameRange()
        {
            return Name?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;
        }

        private IDeclaredElement CreateDeclaration(string declaredName)
        {
            return new PropertyDeclaredElement(declaredName, GetSourceFile(), GetTreeStartOffset().Offset);
        }
    }
}