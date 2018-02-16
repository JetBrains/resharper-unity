using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements
{
    public class ShaderLabDeclaredElementType : DeclaredElementType
    {
        public static readonly ShaderLabDeclaredElementType Property = new ShaderLabDeclaredElementType("Property", PsiSymbolsThemedIcons.Property.Id);

        private readonly IconId myIconId;

        private ShaderLabDeclaredElementType(string name, IconId iconId)
            : base(name)
        {
            PresentableName = name;
            myIconId = iconId;
        }

        public override string PresentableName { get; }
        protected override IconId GetImage() => myIconId;
        public override bool IsPresentable(PsiLanguageType language) => language.Is<ShaderLabLanguage>();
        protected override IDeclaredElementPresenter DefaultPresenter =>
            ShaderLabDeclaredElementPresenter.Instance;
    }
}