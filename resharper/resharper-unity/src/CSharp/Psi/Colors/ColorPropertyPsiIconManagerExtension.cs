using JetBrains.Application.UI.Icons.ColorIcons;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors
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