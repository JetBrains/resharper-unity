#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots;

[GeneratorBuilder(GeneratorUnityKinds.UnityGenerateComponentReferences, typeof(CSharpLanguage))]
public class GenerateComponentReferencesBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
{
    private const string ReferenceType = "ReferenceType";

    protected override void BuildOptions(CSharpGeneratorContext context, ICollection<IGeneratorOption> options)
    {
        base.BuildOptions(context, options);
        options.Add(new GeneratorOptionSelector(ReferenceType,
            Strings.UnityDots_GenerateComponentReference_Component_References,
            new[]
            {
                KnownTypes.RefRO.ShortName,
                KnownTypes.RefRW.ShortName,
                // Should check users reports
                // KnownTypes.EnabledRefRO.ShortName,
                // KnownTypes.EnabledRefRW.ShortName
            }));
    }

    public override double Priority => 101;

    protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
    {
        var selectedClassDeclaration = context.ClassDeclaration;
        var node = context.Anchor;
        var classLikeDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
        if (classLikeDeclaration == null)
            return;

        var refTypeName = GetSelectedRefTypeElement(context);

        var factory = CSharpElementFactory.GetInstance(selectedClassDeclaration);
        var selectedGeneratorElements = context.InputElements.OfType<GeneratorDeclaredElement>();

        foreach (var generatorElement in selectedGeneratorElements)
        {
            var declaredElement = generatorElement.DeclaredElement;

            if (declaredElement is not ITypeElement componentTypeElement)
                continue;

            var typeElementShortName = componentTypeElement.ShortName;
            var uniqueFieldName = NamingUtil.GetUniqueName(classLikeDeclaration.Body, typeElementShortName,
                NamedElementKinds.PrivateInstanceFields);
            var substitution = EmptySubstitution.INSTANCE.Extend(refTypeName.TypeParameters[0],
                TypeFactory.CreateType(componentTypeElement));
            var fieldType = TypeFactory.CreateType(refTypeName, substitution, NullableAnnotation.NotAnnotated);

            var fieldDeclaration = factory.CreateFieldDeclaration(fieldType, uniqueFieldName);
            fieldDeclaration.SetAccessRights(AccessRights.PRIVATE);
            fieldDeclaration.SetReadonly(true);

            using (WriteLockCookie.Create())
            {
                classLikeDeclaration.AddClassMemberDeclaration(fieldDeclaration);
            }
        }
    }

    private static ITypeElement GetSelectedRefTypeElement(CSharpGeneratorContext context)
    {
        var referencedType = context.GetOption(ReferenceType);
        var clrTypeName = referencedType == KnownTypes.RefRO.ShortName ? KnownTypes.RefRO :
            referencedType == KnownTypes.RefRW.ShortName ? KnownTypes.RefRW :
            referencedType == KnownTypes.EnabledRefRO.ShortName ? KnownTypes.EnabledRefRO :
            referencedType == KnownTypes.EnabledRefRW.ShortName ? KnownTypes.EnabledRefRW : EmptyClrTypeName.Instance;
        var (refTypeName, _) =
            TypeFactory.CreateTypeByCLRName(clrTypeName, NullableAnnotation.Unknown, context.PsiModule);
        return refTypeName;
    }
}