using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Search;

[PsiComponent(Instantiation.DemandAnyThreadSafe)]
public class ShaderLabClrDeclaredElementSearchFilter : ISearchFilter
{
    public SearchFilterKind Kind => SearchFilterKind.Language;

    public object TryGetKey(IDeclaredElement declaredElement) => declaredElement as IClrDeclaredElement;

    public bool IsAvailable(SearchPattern pattern) => true;

    public bool CanContainReferences(IPsiSourceFile sourceFile, object key) => !sourceFile.LanguageType.Is<ShaderLabProjectFileType>();
}