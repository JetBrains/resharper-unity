using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    [GeneratorBuilder(GeneratorUnityKinds.UnityGenerateBakerAndAuthoring, typeof(CSharpLanguage))]
    public class GenerateBakerAndAuthoringActionBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
    {
        public override double Priority => 100;
        
        private readonly struct ConversionData
        {
            public readonly  IClrTypeName TypeName;
            public readonly string FunctionTemplate;

            public ConversionData(IClrTypeName typeName, string functionTemplate)
            {
                TypeName = typeName;
                FunctionTemplate = functionTemplate;
            }

            public void Deconstruct(out IClrTypeName typeName, out string functionTemplate)
            {
                typeName = TypeName;
                functionTemplate = FunctionTemplate;
            }
        }

        private const string SelectedBaker = "SelectedBaker";
        private const string GenerateAsNested = "GenerateAsNested";
        
        private static readonly Dictionary<IClrTypeName, ConversionData> ourTypesConversionDictionary = new() 
        {
            {KnownTypes.Entity, new ConversionData(KnownTypes.GameObject, "GetEntity($0.$1)")},
            {KnownTypes.Random, new ConversionData(PredefinedType.UINT_FQN, "Unity.Mathematics.Random.CreateFromIndex($0.$1)")}
        };

        protected override void BuildOptions(CSharpGeneratorContext context, ICollection<IGeneratorOption> options)
        {
            base.BuildOptions(context, options);
            
            options.Add(new GeneratorOptionBoolean(GenerateAsNested, Strings.UnityDots_GenerateBakerAndAuthoring_GenerateBakerAsNestedClass, true));
            
            var (bakerBaseTypeElement, _) = TypeFactory.CreateTypeByCLRName(KnownTypes.Baker, context.PsiModule);
            var typeElements = new List<ITypeElement>();
            context.Solution.GetPsiServices().Finder.FindInheritors(bakerBaseTypeElement, typeElements.ConsumeDeclaredElements(),
                NullProgressIndicator.Instance);

            var availableBakers = new List<string>()
            {
                Strings.UnityDots_GenerateBakerAndAuthoring_NewBaker
            };

            foreach (var typeElement in typeElements)
            {
                availableBakers.Add(typeElement.GetClrName().FullName);
            }
            
            options.Add(new GeneratorOptionSelector(SelectedBaker, Strings.UnityDots_GenerateBakerAndAuthoring_SelectedBaker, availableBakers));
        }

        // Enables/disables the menu item
        protected override bool IsAvailable(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.IsFromUnityProject() && HasUnityBaseType(context) && base.IsAvailable(context);
        }

        protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
        {
            if (!HasUnityBaseType(context)) return;
            var selectedBaker = context.GetOption(SelectedBaker);
            var generateAsNested = context.GetBooleanOption(GenerateAsNested);

            var componentStructDeclaration = context.ClassDeclaration;
            
            var factory = CSharpElementFactory.GetInstance(componentStructDeclaration);
            var componentName = componentStructDeclaration.DeclaredName;
            var componentToAuthoringFieldNames = new Dictionary<string, string>();
            
            var (declaredAuthoringType, authoringDeclaration) = GenerateAuthoringDeclaration(context, componentName, componentStructDeclaration, factory, ref componentToAuthoringFieldNames);
            Func<IClassLikeDeclaration, IClassLikeDeclaration> insertionFunc = generateAsNested
                ? declaration => authoringDeclaration.AddClassMemberDeclaration(declaration)
                : declaration => ModificationUtil.AddChildAfter(componentStructDeclaration, declaration);
            
            Func<string, string> bakerNameGenerator = generateAsNested
                ? bakerClassName => $"{authoringDeclaration.CLRName}+{bakerClassName}"
                : bakerClassName => componentStructDeclaration.CLRName.Replace(componentStructDeclaration.DeclaredName, bakerClassName);

            GenerateBaker(context, factory, componentName, componentStructDeclaration, declaredAuthoringType, componentToAuthoringFieldNames, bakerNameGenerator, insertionFunc);
        }

        private static void GenerateBaker(CSharpGeneratorContext context, CSharpElementFactory factory,
            string componentName,
            IClassLikeDeclaration componentStructDeclaration,
            IDeclaredType declaredAuthoringType, Dictionary<string, string> componentToAuthoringFieldNames,
            Func<string, string> bakerNameGenerator,
            Func<IClassLikeDeclaration, IClassLikeDeclaration> insertionDelegate)
        {
            var componentDeclaredType = componentStructDeclaration.DeclaredElement;// TypeFactory.CreateType(componentStructDeclaration.DeclaredElement);

            var bakerClassDeclaration = GetOrCreateBakerClassDeclaration(context, factory, componentName, componentStructDeclaration, declaredAuthoringType, bakerNameGenerator, insertionDelegate);
            var bakeMethodExpression = GetOrCreateBakeMethodExpression(bakerClassDeclaration, factory, declaredAuthoringType, out var authoringParameterName);
            var componentCreationExpression = GetOrCreateComponentCreationExpression(factory, bakeMethodExpression, componentDeclaredType);
            var creationExpressionInitializer = GetOrCreateInitializer(componentCreationExpression, factory);

            //remove all member initialization
            foreach (var initializer in creationExpressionInitializer.MemberInitializers)
                creationExpressionInitializer.RemoveMemberInitializer(initializer);
        
            var selectedGeneratorElements = context.InputElements.OfType<GeneratorDeclaredElement>();
            
            foreach (var generatorElement in selectedGeneratorElements)
            {
                if (!(generatorElement.DeclaredElement is IField selectedField))
                    continue;

                var fieldTypeName = selectedField.Type.GetTypeElement()?.GetClrName();
                Assertion.AssertNotNull(fieldTypeName);
                var fieldShortName = selectedField.ShortName;
                var authoringFieldName = componentToAuthoringFieldNames[fieldShortName];
                
                var authoringFieldType = TypeFactory.CreateTypeByCLRName(fieldTypeName, NullableAnnotation.NotAnnotated, selectedField.Module);

                var initializationFormat = "$0.$1";
                if (ourTypesConversionDictionary.TryGetValue(authoringFieldType.GetClrName(), out var conversionData))
                    initializationFormat = conversionData.FunctionTemplate;
                
                creationExpressionInitializer.AddMemberInitializerBefore(factory.CreateObjectPropertyInitializer(
                    fieldShortName,
                    factory.CreateExpression(initializationFormat, authoringParameterName, authoringFieldName)), null);
            }

            componentCreationExpression.RemoveArgumentList();
            componentCreationExpression.FormatNode(CodeFormatProfile.COMPACT);
        }

        private static IClassLikeDeclaration GetOrCreateBakerClassDeclaration(CSharpGeneratorContext context,
            CSharpElementFactory factory, string componentName, IClassLikeDeclaration componentStructDeclaration,
            IDeclaredType declaredAuthoringType, Func<string, string> bakerDeclaredName, Func<IClassLikeDeclaration, IClassLikeDeclaration> insertionFunc)
        {
            // get parent class 'bakerTypeWithSubstitution' : Baker<ComponentNameAuthoring>
            var bakerGenericBaseClass = TypeFactory.CreateTypeByCLRName(KnownTypes.Baker, NullableAnnotation.NotAnnotated, context.PsiModule);
            var bakerTypeElement = bakerGenericBaseClass.GetTypeElement().NotNull();
            var substitution = EmptySubstitution.INSTANCE.Extend(bakerTypeElement.TypeParameters[0], declaredAuthoringType);
            var bakerTypeWithSubstitution = TypeFactory.CreateType(bakerTypeElement, substitution, NullableAnnotation.NotAnnotated);

            //Create class 'ComponentDataBaker : Baker<ComponentNameAuthoring>'
            var bakerClassName = $"{componentName}Baker";
            var declaredName = bakerDeclaredName.Invoke(bakerClassName);
            
            var (existingBakerTypeElement, _) = TypeFactory.CreateTypeByCLRName(declaredName, context.PsiModule);

            IClassLikeDeclaration bakerClassDeclaration;
            //Must be derived from bakerTypeWithSubstitution
            if (existingBakerTypeElement != null && existingBakerTypeElement.IsDescendantOf(bakerTypeWithSubstitution.GetTypeElement()))
            {
                bakerClassDeclaration = existingBakerTypeElement.GetDeclarations().FirstOrDefault() as IClassDeclaration;
                Assertion.AssertNotNull(bakerClassDeclaration);
            }
            else
            {
                bakerClassName = NamingUtil.GetUniqueName(componentStructDeclaration, bakerClassName, NamedElementKinds.TypesAndNamespaces);
                bakerClassDeclaration =
                    (IClassDeclaration)factory.CreateTypeMemberDeclaration("public class $0 : $1 { }", bakerClassName,
                        bakerTypeWithSubstitution);

                using (WriteLockCookie.Create())
                {

                    bakerClassDeclaration = insertionFunc(bakerClassDeclaration);// ModificationUtil.AddChildAfter(insertionFunc, bakerClassDeclaration);
                    bakerClassDeclaration.FormatNode(CodeFormatProfile.DEFAULT);
                }
            }
            
            return bakerClassDeclaration;
        }

        private static IMethodDeclaration GetOrCreateBakeMethodExpression(IClassLikeDeclaration bakerClassDeclaration,
            CSharpElementFactory factory,
            IDeclaredType declaredAuthoringType, out string authoringParameterName)
        {
            //'public override void Bake(ComponentNameAuthoring authoring)'
            const string bakeMethodName = "Bake";
            authoringParameterName = "authoring";

            //TODO: maybe check if implements void Baker<T>::Bake(T) 
            var existingBakeMethodDeclaration = bakerClassDeclaration.MethodDeclarations.FirstOrDefault(m => m.DeclaredName.Equals(bakeMethodName));
            if(existingBakeMethodDeclaration is { IsOverride: true } && existingBakeMethodDeclaration.Type.IsVoid())
            {
                var parameters = existingBakeMethodDeclaration.DeclaredElement.NotNull().Parameters;
                if (parameters.Count == 1 &&
                    (parameters[0].Type.GetTypeElement()?.Equals(declaredAuthoringType.GetTypeElement()) ?? false))
                {
                    authoringParameterName = parameters[0].ShortName;
                    return existingBakeMethodDeclaration;
                }
            }
            
            var bakeMethodExpression =
                (IMethodDeclaration)factory.CreateTypeMemberDeclaration("void $0($1 $2) {}", bakeMethodName, declaredAuthoringType,
                    authoringParameterName);
            bakeMethodExpression.SetOverride(true);
            bakeMethodExpression.SetAccessRights(AccessRights.PUBLIC);
            bakeMethodExpression = bakerClassDeclaration.AddClassMemberDeclaration(bakeMethodExpression);
            return bakeMethodExpression;
        }

        private static IObjectCreationExpression GetOrCreateComponentCreationExpression(CSharpElementFactory factory,
            IMethodDeclaration bakeMethodExpression, ITypeElement componentDeclaredType)
        {
            var existingCreationExpression = bakeMethodExpression.Body.FindNextNode( node => (node is IObjectCreationExpression expression
                && componentDeclaredType.Equals(expression.Type().GetTypeElement()))? TreeNodeActionType.ACCEPT : TreeNodeActionType.CONTINUE);

            if (existingCreationExpression != null)
                return (IObjectCreationExpression)existingCreationExpression;

            //AddComponent(new ComponentData{})
            var addComponentStatement =
                (IExpressionStatement)bakeMethodExpression.Body.AddStatementAfter(factory.CreateStatement("AddComponent();"),
                    null);
            var addComponentExpression = (addComponentStatement.Expression as IInvocationExpression).NotNull();
            var creationArgument = addComponentExpression.AddArgumentAfter(
                factory.CreateArgument(ParameterKind.VALUE, factory.CreateExpression("new $0()", componentDeclaredType)), null);

            var componentCreationExpression = (IObjectCreationExpression)creationArgument.Value;
            return componentCreationExpression;
        }

        private static IObjectInitializer GetOrCreateInitializer(IObjectCreationExpression objectCreationExpression, CSharpElementFactory elementFactory)
        {
            var initializer = objectCreationExpression.Initializer;
            if (initializer is IObjectInitializer objectInitializer) 
                return objectInitializer;

            return (IObjectInitializer)objectCreationExpression.SetInitializer(elementFactory.CreateObjectInitializer());
        }

        private static (IDeclaredType, IClassLikeDeclaration) GenerateAuthoringDeclaration(CSharpGeneratorContext context, string componentName,
            IClassLikeDeclaration componentStructDeclaration,
            CSharpElementFactory factory,
            ref Dictionary<string, string> componentToAuthoringFieldNames)
        {
            var authoringDeclaration = GetOrCreateAuthoringClassDeclaration(context, componentName, componentStructDeclaration, factory);

            var selectedGeneratorElements = context.InputElements.OfType<GeneratorDeclaredElement>();
            var existingFields =  authoringDeclaration.DeclaredElement.NotNull().Fields.ToDictionary(f => f.ShortName, f => f);
            foreach (var generatorElement in selectedGeneratorElements)
            {  
                if (!(generatorElement.DeclaredElement is IField selectedField)) 
                    continue;
                
                var fieldShortName = selectedField.ShortName;
                var authoringFieldType = GetFieldType(selectedField);
                Assertion.AssertNotNull(authoringFieldType);

                if (existingFields.TryGetValue(fieldShortName, out var existingField))
                {
                    //Same field with same type
                    if (existingField.Type.Equals(authoringFieldType))
                    {
                        componentToAuthoringFieldNames.Add(fieldShortName, fieldShortName);
                        continue;
                    }
                    else
                    {
                        // TODO - for further refactoring feature: replace, delete, etc.
                    }
                }
                
                //Add field to Authoring class
                var authoringFieldName = NamingUtil.GetUniqueName(authoringDeclaration.Body, fieldShortName, NamedElementKinds.PublicFields, null,
                    element =>
                    {
                        return existingFields.ContainsKey(element.ShortName);
                    });
                componentToAuthoringFieldNames.Add(fieldShortName, authoringFieldName);
                
                var fieldDeclaration = factory.CreateFieldDeclaration(authoringFieldType, authoringFieldName);
                fieldDeclaration.SetAccessRights(AccessRights.PUBLIC);
                authoringDeclaration.AddClassMemberDeclaration(fieldDeclaration);
            }
            
            return (TypeFactory.CreateType(authoringDeclaration!.DeclaredElement), authoringDeclaration);
        }

        private static IClassDeclaration GetOrCreateAuthoringClassDeclaration(CSharpGeneratorContext context, string componentName, IClassLikeDeclaration componentStructDeclaration, CSharpElementFactory factory)
        {
            // public class ComponentNameAuthoring : MonoBehaviour {}
            var authoringMonoBehName = $"{componentName}Authoring";

            var declaredName =
                componentStructDeclaration.CLRName.Replace(componentStructDeclaration.DeclaredName, authoringMonoBehName);
            var (existingAuthoringTypeElement, _) = TypeFactory.CreateTypeByCLRName(declaredName, context.PsiModule);

            IClassDeclaration authoringDeclaration;
            //Must be derived from MonoBehaviour
            if (existingAuthoringTypeElement != null && existingAuthoringTypeElement.DerivesFrom(KnownTypes.MonoBehaviour))
            {
                authoringDeclaration = existingAuthoringTypeElement.GetDeclarations().FirstOrDefault() as IClassDeclaration;
                Assertion.AssertNotNull(authoringDeclaration);
            }
            else
            {
                authoringMonoBehName = NamingUtil.GetUniqueName(componentStructDeclaration, authoringMonoBehName, NamedElementKinds.TypesAndNamespaces);

                authoringDeclaration = factory.CreateTypeMemberDeclaration("public class $0 : $1{}", authoringMonoBehName,
                    TypeFactory.CreateTypeByCLRName(KnownTypes.MonoBehaviour, NullableAnnotation.NotAnnotated, context.PsiModule)) as IClassDeclaration;
                Assertion.AssertNotNull(authoringDeclaration);
                using (WriteLockCookie.Create())
                    authoringDeclaration = ModificationUtil.AddChildAfter(componentStructDeclaration, authoringDeclaration);
            }

            return authoringDeclaration;
        }

        private static IType GetFieldType(IField selectedField)
        {
            var fieldTypeName = selectedField.Type.GetTypeElement().NotNull().GetClrName();
            var authoringFieldType = TypeFactory.CreateTypeByCLRName(fieldTypeName, NullableAnnotation.NotAnnotated, selectedField.Module);

            if (ourTypesConversionDictionary.TryGetValue(authoringFieldType.GetClrName(), out var result))
                return TypeFactory.CreateTypeByCLRName(result.TypeName, NullableAnnotation.NotAnnotated, selectedField.Module);
            
            return authoringFieldType;
        }

        private static bool HasUnityBaseType(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.DeclaredElement is IStruct typeElement && 
                   (UnityApi.IsDerivesFromIComponentData(typeElement) || UnityApi.IsDerivesFromIAspect(typeElement));
        }
    }
}