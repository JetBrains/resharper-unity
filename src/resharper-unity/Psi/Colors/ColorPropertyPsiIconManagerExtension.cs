using JetBrains.ReSharper.Psi;
using JetBrains.UI.Icons;

#if WAVE07 || WAVE08
using JetBrains.UI.Icons.ColorIcons;
#else
using JetBrains.Application.UI.Icons.ColorIcons;
#endif

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Colors
{
    [DeclaredElementIconProvider]
    public class ColorPropertyPsiIconManagerExtension : IDeclaredElementIconProvider
    {
        public IconId GetImageId(IDeclaredElement declaredElement, PsiLanguageType languageType, out bool canApplyExtensions)
        {
            canApplyExtensions = false;

            var typeMember = declaredElement as ITypeMember;
            if (typeMember == null) return null;

            if (!UnityColorTypes.IsColorProperty(typeMember)) return null;

            var color = UnityNamedColors.Get(typeMember.ShortName);
            if (color == null) return null;

            return new ColorIconId(color.Value);
        }
    }
}