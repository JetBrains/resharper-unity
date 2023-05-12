#nullable enable
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.QuickDoc
{
    [DeclaredElementDescriptionProvider]
    public class ShaderLabDeclaredElementDescriptionProvider : IDeclaredElementDescriptionProvider
    {
        public RichTextBlock? GetElementDescription(IDeclaredElement element, DeclaredElementDescriptionStyle style, PsiLanguageType language, IPsiModule module = null)
        {
            var type = element.GetElementType();
            return type switch
            {
                _ when type == ShaderLabDeclaredElementType.Command => new RichTextBlock($"ShaderLab command {element.ShortName}"), 
                _ => null
            };
        }

        public bool? IsElementObsolete(IDeclaredElement element, out RichTextBlock? obsoleteDescription, DeclaredElementDescriptionStyle style)
        {
            obsoleteDescription = null;
            return false;
        }

        public int Priority => 0;
    }
}
