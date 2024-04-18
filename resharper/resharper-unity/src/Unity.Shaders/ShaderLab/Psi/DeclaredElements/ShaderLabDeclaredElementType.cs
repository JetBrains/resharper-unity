#nullable enable

using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements
{
    public class ShaderLabDeclaredElementType : DeclaredElementType
    {
        public static readonly ShaderLabDeclaredElementType Property = new("Property", PsiSymbolsThemedIcons.Property.Id);
        public static readonly ShaderLabDeclaredElementType Command = new("Command", PsiSymbolsThemedIcons.Field.Id);
        public static readonly ShaderLabDeclaredElementType BlockCommand = new("BlockCommand", PsiSymbolsThemedIcons.Struct.Id);
        public static readonly ShaderLabDeclaredElementType PropertiesCommand = new("PropertiesCommand", PsiSymbolsThemedIcons.Property.Id);
        public static readonly ShaderLabDeclaredElementType ShaderCommand = new("ShaderCommand", PsiSymbolsThemedIcons.FileShader.Id);
        public static readonly ShaderLabDeclaredElementType TexturePassDef = new("TexturePassDef", PsiSymbolsThemedIcons.ShaderPass.Id);
        public static readonly ShaderLabDeclaredElementType SubShaderCommand = new("SubShaderCommand", PsiSymbolsThemedIcons.SubShader.Id);
        public static readonly ShaderLabDeclaredElementType IncludeBlock = new("IncludeBlock", PsiSymbolsThemedIcons.Pipeline.Id);        
        public static readonly ShaderLabDeclaredElementType ProgramBlock = new("ProgramBlock", PsiSymbolsThemedIcons.Pipeline.Id);
        public static readonly ShaderLabDeclaredElementType Shader = new("Shader", null);
        public static readonly ShaderLabDeclaredElementType TexturePass = new("TexturePass", PsiSymbolsThemedIcons.ShaderPass.Id);

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