#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    [GeneratorBuilder(GeneratorUnityKinds.UnityGenerateRefAccessors, typeof(CSharpLanguage))]
    public class GenerateRefFieldsAccessorsBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
    {
        private const string GenerateSetters = "GenerateSetters";
        private const string RefGetterFormat = "$0.ValueRO.$1;";
        private const string RefSetterFormat = "$0.ValueRW.$1 = value;";
        private const string AspectGetterFormat = "$0.$1;";
        private const string AspectSetterFormat = "$0.$1 = value;";

        protected override void BuildOptions(CSharpGeneratorContext context, ICollection<IGeneratorOption> options)
        {
            base.BuildOptions(context, options);
            var node = context.Anchor;
            if (node == null)
               return; 
            
            var (referencedType, isReadOnly) = UnityApiExtensions.GetReferencedType(node.GetContainingNode<IFieldDeclaration>());
            if (referencedType == null)
                return;

            if (isReadOnly)
                return;

            var generateSetters = new GeneratorOptionBoolean(GenerateSetters,
                Strings.UnityDots_GenerateRefAccessors_Generate_Setters, true);
            options.Add(generateSetters);
        }

        public override double Priority => 101;

        protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
        {
            var selectedClassDeclaration = context.ClassDeclaration;
            var node = context.Anchor;
            var classLikeDeclaration = node?.GetContainingNode<IClassLikeDeclaration>();
            if (classLikeDeclaration == null)
                return;

            var fieldDeclaration = node?.GetContainingNode<IFieldDeclaration>();
            if (fieldDeclaration == null)
                return;

            var (referencedType, isReadOnly) = UnityApiExtensions.GetReferencedType(fieldDeclaration);
            if (referencedType == null)
                return;

            var shouldGenerateSetters = context.GetBooleanOption(GenerateSetters);
            var fieldName = fieldDeclaration.DeclaredElement;
            var factory = CSharpElementFactory.GetInstance(selectedClassDeclaration);

            var selectedGeneratorElements = context.InputElements.OfType<GeneratorDeclaredElement>();

            var isDerivesFromIAspect = referencedType.DerivesFrom(KnownTypes.IAspect);
            var getterFormat = isDerivesFromIAspect ? AspectGetterFormat : RefGetterFormat;
            var setterFormat = isDerivesFromIAspect ? AspectSetterFormat : RefSetterFormat;

            foreach (var generatorElement in selectedGeneratorElements)
            {
                var declaredElement = generatorElement.DeclaredElement;

                var fieldOrProperty = declaredElement as IField ?? (ITypeOwner?)(declaredElement as IProperty);

                if (fieldOrProperty == null)
                    continue;

                var fieldShortName = fieldOrProperty.ShortName;
                var generatedPropertyName = BakerGeneratorUtils.CalculateValueFieldName(fieldShortName, fieldOrProperty.GetContainingType()?.ShortName);
                var uniquePropertyName = NamingUtil.GetUniqueName(classLikeDeclaration.Body, generatedPropertyName, NamedElementKinds.Property);

                var propertyDeclaration = factory.CreatePropertyDeclaration(fieldOrProperty.Type, uniquePropertyName);
                propertyDeclaration.SetAccessRights(AccessRights.PUBLIC);
                
                var getterExpression = factory.CreateExpression(getterFormat, fieldName, fieldShortName);
                var generateGettersOnly = isReadOnly || !shouldGenerateSetters;
                
                if (isDerivesFromIAspect)
                {
                    switch (fieldOrProperty)
                    {
                        case IField field:
                            generateGettersOnly |= field.IsReadonly; 
                            break;
                        case IProperty property:
                            generateGettersOnly |= property.IsReadonly || property.Setter == null;
                            break;
                    }
                }

                if (generateGettersOnly)
                {
                    propertyDeclaration.SetBodyExpression(getterExpression);
                }
                else
                {
                    var getterBodyExpression = factory.CreateAccessorDeclaration(AccessorKind.GETTER, false);
                    getterBodyExpression.SetBodyExpression(getterExpression);
                    propertyDeclaration.AddAccessorDeclarationBefore(getterBodyExpression, null);
                    var setterBodyExpression = factory.CreateAccessorDeclaration(AccessorKind.SETTER, false);
                    var setterExpression = factory.CreateExpression(setterFormat, fieldName, fieldShortName);
                    setterBodyExpression.SetBodyExpression(setterExpression);
                    propertyDeclaration.AddAccessorDeclarationBefore(setterBodyExpression, null);
                }

                using (WriteLockCookie.Create())
                {
                    classLikeDeclaration.AddClassMemberDeclarationAfter(propertyDeclaration, fieldDeclaration);
                }
            }
        }
    }
}