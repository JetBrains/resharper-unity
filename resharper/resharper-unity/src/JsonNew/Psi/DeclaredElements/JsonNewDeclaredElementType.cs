using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.DeclaredElements
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
    }
}