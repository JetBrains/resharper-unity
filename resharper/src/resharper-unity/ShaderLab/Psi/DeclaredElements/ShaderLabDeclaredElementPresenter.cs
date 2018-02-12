using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements
{
    [PsiSharedComponent]
    public class ShaderLabDeclaredElementPresenter : IDeclaredElementPresenter
    {
        public string Format(DeclaredElementPresenterStyle style, IDeclaredElement element, ISubstitution substitution,
            out DeclaredElementPresenterMarking marking)
        {
            marking = new DeclaredElementPresenterMarking();
            if (!(element is IShaderLabDeclaredElement))
                return null;

            // TODO: Type (Float, Color, etc.), display name, etc.
            marking.NameRange = new TextRange(0, element.ShortName.Length);
            return element.ShortName;
        }

        public string Format(ParameterKind parameterKind) => string.Empty;
        public string Format(AccessRights accessRights) => string.Empty;
    }
}