using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.DeclaredElements;

public class UxmlDeclaredElementType : DeclaredElementType
{
    public static readonly UxmlDeclaredElementType NamespaceAlias = new("NamespaceAlias", UnityFileTypeThemedIcons.UxmlNamespaceAlias.Id);
    
    private readonly IconId myImageName;

    private UxmlDeclaredElementType(string name, IconId imageName) : base(name)
    {
        PresentableName = name;
        myImageName = imageName;
    }

    public override string PresentableName { get; }

    public override IconId GetImage() => myImageName;

    protected override IDeclaredElementPresenter DefaultPresenter
    {
        get { return UxmlDeclaredElementPresenter.Instance; }
    }

    public override bool IsPresentable(PsiLanguageType language)
    {
        return language.Is<UxmlLanguage>();
    }
}