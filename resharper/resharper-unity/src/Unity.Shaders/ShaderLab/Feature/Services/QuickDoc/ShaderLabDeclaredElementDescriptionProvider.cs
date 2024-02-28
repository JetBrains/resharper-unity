#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.QuickDoc
{
    [DeclaredElementDescriptionProvider(Instantiation.DemandAnyThreadSafe)]
    public class ShaderLabDeclaredElementDescriptionProvider : IDeclaredElementDescriptionProvider
    {
        public RichTextBlock? GetElementDescription(IDeclaredElement element, DeclaredElementDescriptionStyle style, PsiLanguageType language, IPsiModule? module = null) =>
            element switch
            {
                IShaderLabCommandDeclaredElement => new RichTextBlock(DeclaredElementPresenter.Format(language, DeclaredElementPresenter.QUALIFIED_NAME_PRESENTER, element).Prepend("ShaderLab command ")),
                _ => null
            };

        public bool? IsElementObsolete(IDeclaredElement element, out RichTextBlock? obsoleteDescription, DeclaredElementDescriptionStyle style)
        {
            obsoleteDescription = null;
            return false;
        }

        public int Priority => 0;
    }
}
