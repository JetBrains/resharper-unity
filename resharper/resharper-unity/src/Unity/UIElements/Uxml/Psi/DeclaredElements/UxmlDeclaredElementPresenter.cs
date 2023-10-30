using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.DeclaredElements;

public class UxmlDeclaredElementPresenter : IDeclaredElementPresenter
{
    public static readonly UxmlDeclaredElementPresenter Instance = new();

    private UxmlDeclaredElementPresenter()
    {
    }

    public RichText Format(DeclaredElementPresenterStyle style, IDeclaredElement element, ISubstitution substitution,
        out DeclaredElementPresenterMarking marking)
    {
        var celement = (UxmlNamespaceAliasAttribute)element;
        marking = new DeclaredElementPresenterMarking();
        return new RichText(celement.ShortName);
    }

    public string Format(ParameterKind parameterKind)
    {
        return string.Empty;
    }

    public string Format(AccessRights accessRights)
    {
        return string.Empty;
    }

    public string GetEntityKind(IDeclaredElement declaredElement)
    {
        return string.Empty;
    }
}