using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements
{
    public static class ShaderLabDeclaredElementPresenterStyles
    {
        public static readonly DeclaredElementPresenterStyle CANDIDATE_PRESENTER;

        static ShaderLabDeclaredElementPresenterStyles()
        {
            CANDIDATE_PRESENTER = new DeclaredElementPresenterStyle
            {
                ShowName = NameStyle.QUALIFIED,
                ShowType = TypeStyle.DEFAULT
            };
        }
    }
}