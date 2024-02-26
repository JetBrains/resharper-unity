#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots;

[GeneratorElementProvider(GeneratorUnityKinds.UnityGenerateComponentReferences, typeof(CSharpLanguage))]
public class GenerateComponentReferencesProvider : GeneratorProviderBase<CSharpGeneratorContext>
{
    public override double Priority => 102;

    public override void Populate(CSharpGeneratorContext context)
    {
        if (!context.ClassDeclaration.IsFromUnityProject())
            return;
        var node = context.Anchor;

        var sourceType = node?.GetContainingNode<IClassLikeDeclaration>()?.DeclaredElement;

        if (sourceType == null)
            return;
        
        var (componentDataBaseTypeElement, _) = TypeFactory.CreateTypeByCLRName(KnownTypes.IComponentData, NullableAnnotation.Unknown, context.PsiModule);
        var typeElements = new List<ITypeElement>();

        var solution = context.Solution;
        var finder = solution.GetPsiServices().Finder;
        finder.FindInheritors(componentDataBaseTypeElement,  typeElements.ConsumeDeclaredElements(), NullProgressIndicator.Create());
       
        foreach (var typeElement in typeElements.OrderBy(element => element.GetClrName().ShortName))
        {
            if (typeElement == null || !typeElement.IsFromUnityProject() || typeElement is not IStruct) 
                continue;

            context.ProvidedElements.Add(new GeneratorDeclaredElement(typeElement));
        }
    }
}