using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Json.Psi.DeclaredElements
{
    public class JsonNewDeclaredElementType : DeclaredElementType
    {
        private readonly IconId myIconId;

        protected JsonNewDeclaredElementType(string name, IconId iconId)
            : base(name)
        {
            PresentableName = name;
            myIconId = iconId;
        }

        public override string PresentableName { get; }
        protected override IDeclaredElementPresenter DefaultPresenter => JsonNewDeclaredElementPresenter.Instance;
        public override IconId GetImage() => myIconId;
        public override bool IsPresentable(PsiLanguageType language) => language.Is<JsonNewLanguage>();

        // Obsolete, but the suggested alternative method is missing, and this is still in use in the platform
#pragma warning disable CS0618
        public virtual bool IsValidName(string name) => NamingUtil.IsIdentifier(name);
#pragma warning restore CS0618
    }
}