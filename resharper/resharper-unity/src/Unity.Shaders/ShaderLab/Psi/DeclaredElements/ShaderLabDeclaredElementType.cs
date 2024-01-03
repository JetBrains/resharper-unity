#nullable enable

using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public class ShaderLabDeclaredElementType : DeclaredElementType
    {
        public static readonly ShaderLabDeclaredElementType Property = new("Property", PsiSymbolsThemedIcons.Property.Id);
        public static readonly ShaderLabDeclaredElementType Command = new("Command", null);
        public static readonly ShaderLabDeclaredElementType IncludeBlock = new("IncludeBlock", null);
        public static readonly ShaderLabDeclaredElementType ProgramBlock = new("ProgramBlock", null);
        public static readonly ShaderLabDeclaredElementType Shader = new("Shader", PsiSymbolsThemedIcons.FileShader.Id);
        public static readonly ShaderLabDeclaredElementType TexturePass = new("TexturePass", null);
        public static readonly ShaderLabDeclaredElementType Category = new("Category", null);
        public static readonly ShaderLabDeclaredElementType GrabPass = new("GrabPass", null);

        private readonly IconId? myIconId;

        private ShaderLabDeclaredElementType(string name, IconId? iconId)
            : base(name)
        {
            PresentableName = name;
            myIconId = iconId;
        }

        public override string PresentableName { get; }
        public override IconId? GetImage() => myIconId;
        public override bool IsPresentable(PsiLanguageType language) => language.Is<ShaderLabLanguage>();
        protected override IDeclaredElementPresenter DefaultPresenter => ShaderLabDeclaredElementPresenter.Instance;
    }
}