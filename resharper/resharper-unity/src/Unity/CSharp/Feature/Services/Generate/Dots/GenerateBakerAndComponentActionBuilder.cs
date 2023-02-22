#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    [GeneratorBuilder(GeneratorUnityKinds.UnityGenerateBakerAndComponent, typeof(CSharpLanguage))]
    public class GenerateBakerAndComponentActionBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
    {
        public override double Priority => 100;
        private const string SelectedComponent = "SelectedComponent";
        private const string SelectedBaker = "Selectedbaker";
        private readonly Dictionary<string, ITypeElement> myExistedComponents = new(100);
        private readonly Dictionary<string, ITypeElement> myExistedBakers = new(100);

        protected override void BuildOptions(CSharpGeneratorContext context, ICollection<IGeneratorOption> options)
        {
            base.BuildOptions(context, options);
            
            var solution = context.Solution;
            var packageManager = solution.GetComponent<PackageManager>();
            var finder = solution.GetPsiServices().Finder;

            var availableComponents = GetAvailableComponents(finder, packageManager, context);

            var componentsSelector = new GeneratorOptionSelector(SelectedComponent, Strings.UnityDots_GenerateBakerAndComponent_ComponentData, availableComponents.ToIReadOnlyList())
                { Value = Strings.UnityDots_GenerateBakerAndComponent_NewComponentData };

            options.Add(componentsSelector);
            
            var existingBakers = TryGetExistingBakers(context.ClassDeclaration, context, finder);

            var bakersSelector = new GeneratorOptionSelector(SelectedBaker, Strings.UnityDots_GenerateBakerAndAuthoring_Baker, existingBakers)
            { Value = existingBakers.SingleItem() };

            options.Add(bakersSelector);
        }

        private HashSet<string> GetAvailableComponents(IFinder finder, PackageManager packageManager, IGeneratorContext context)
        {
            var (componentDataBaseTypeElement, _) = TypeFactory.CreateTypeByCLRName(KnownTypes.IComponentData, NullableAnnotation.Unknown, context.PsiModule);
            var typeElements = new List<ITypeElement>();
            finder.FindInheritors(componentDataBaseTypeElement, typeElements.ConsumeDeclaredElements(),
                NullProgressIndicator.Create());

            var availableComponents = new HashSet<string> { Strings.UnityDots_GenerateBakerAndComponent_NewComponentData };
            myExistedComponents.Clear();

            foreach (var typeElement in typeElements)
            {
                if (!typeElement.IsFromUnityProject())
                    continue;
                
                var packageData = packageManager.GetOwningPackage(typeElement.GetSingleOrDefaultSourceFile().GetLocation());
                if(packageData != null && packageData.Source != PackageSource.Local)
                    continue;

                var name = typeElement.GetClrName().FullName;
                availableComponents.Add(name);
                myExistedComponents[name] = typeElement;
            }

            return availableComponents;
        }

        private  IReadOnlyList<string> TryGetExistingBakers(IClassLikeDeclaration authoringDeclaration,
            IGeneratorContext context, IFinder finder)
        {
            var bakerGenericBaseClass = TypeFactory.CreateTypeByCLRName(KnownTypes.Baker, NullableAnnotation.NotAnnotated, context.PsiModule);
            var bakerTypeElement = bakerGenericBaseClass.GetTypeElement().NotNull();
            IType declaredAuthoringType = TypeFactory.CreateType(authoringDeclaration.DeclaredElement!);
            var authoringSubstitutions = EmptySubstitution.INSTANCE.Extend(bakerTypeElement.TypeParameters[0], declaredAuthoringType);
            var bakerTypeWithSubstitution = TypeFactory.CreateType(bakerTypeElement, authoringSubstitutions, NullableAnnotation.NotAnnotated);

            var typeElements = new List<ITypeElement>();
            myExistedBakers.Clear();
            var bakerTypeElementWithSubs = bakerTypeWithSubstitution.GetTypeElement();
            if (bakerTypeElementWithSubs == null)
                return EmptyList<string>.Instance;
            
            finder.FindInheritors(bakerTypeElementWithSubs, typeElements.ConsumeDeclaredElements(), NullProgressIndicator.Create());

            var result = new List<string>(typeElements.Count);

            foreach (var typeElement in typeElements)
            {
                var declaredTypes = typeElement.GetSuperTypes();
                if (declaredTypes.Contains(bakerTypeWithSubstitution))
                {
                    var fullName = typeElement.GetClrName().FullName;
                    myExistedBakers[fullName] = typeElement;
                    result.Add(fullName);
                }
            }
            result.Add(Strings.UnityDots_GenerateBakerAndAuthoring_NewBaker);
            
            return result;
        }

        // Enables/disables the menu item
        protected override bool IsAvailable(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.IsFromUnityProject() && HasUnityBaseType(context) && base.IsAvailable(context);
        }

        // provides baker generation for empty Component
        protected override bool HasProcessableElements(CSharpGeneratorContext context, IEnumerable<IGeneratorElement> elements)
        {
            return true;
        }

        protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
        {
            if (!HasUnityBaseType(context)) 
                return;
            
            var selectedComponentData = GetSelectedComponent(context);

            var componentToAuthoringFieldNames = new Dictionary<string, string>(context.InputElements.Count);
            var authoringDeclaration = context.ClassDeclaration;
            var factory = CSharpElementFactory.GetInstance(authoringDeclaration);
            
            var componentGenerationInfo = new ComponentDataGenerationInfo(selectedComponentData, authoringDeclaration, factory); 
            var componentDataGenerationResult = GenerateComponentDataDeclaration(context, componentGenerationInfo, ref componentToAuthoringFieldNames);

            var selectedBaker = GetSelectedBaker(context);

            var bakerGenerationInfo = new BakerGenerationInfo(selectedBaker, 
                componentDataGenerationResult, factory, context.PsiModule);
            GenerateBaker(context, componentToAuthoringFieldNames, bakerGenerationInfo);
        }

        private ITypeElement? GetSelectedComponent(IGeneratorContext context)
        {
            return TryGetSelectedClass(context, SelectedComponent, myExistedComponents);
        }
        
        private ITypeElement? GetSelectedBaker(IGeneratorContext context)
        {
            return TryGetSelectedClass(context, SelectedBaker, myExistedBakers);
        }

        private static ITypeElement? TryGetSelectedClass(IGeneratorContext context, string selectionName, Dictionary<string, ITypeElement> typesCache)
        {
            var selectedTypeName = context.GetOption(selectionName);
            if (string.IsNullOrEmpty(selectedTypeName))
                return null;
            if (typesCache.TryGetValue(selectedTypeName, out var componentData))
                return componentData;

            return null;
        }

        private static void GenerateBaker(IGeneratorContext context, Dictionary<string, string> componentToAuthoringFieldNames, BakerGenerationInfo generationInfo)
        {
            var bakerClassDeclarations = generationInfo.ExistedBaker != null 
                ? generationInfo.ExistedBaker.GetDeclarations().OfType<IClassLikeDeclaration>().ToArray()
                : CreateBakerClassDeclaration(generationInfo);
            
            var bakeMethodExpression = GetOrCreateBakeMethodExpression(bakerClassDeclarations, generationInfo.Factory, generationInfo, out var authoringParameterName);
            var componentCreationExpression = GetOrCreateComponentCreationExpression(generationInfo.Factory, bakeMethodExpression, generationInfo.ComponentDataDeclaration.DeclaredElement!);
            if(context.InputElements.Count != 0)
            {
                var creationExpressionInitializer = GetOrCreateInitializer(componentCreationExpression, generationInfo.Factory);

                //remove all member initialization
                foreach (var initializer in creationExpressionInitializer.MemberInitializers)
                    creationExpressionInitializer.RemoveMemberInitializer(initializer);
        
                var selectedGeneratorElements = context.InputElements.OfType<GeneratorDeclaredElement>();
                foreach (var generatorElement in selectedGeneratorElements)
                {
                    if (generatorElement.DeclaredElement is not IField selectedField)
                        continue;

                    var fieldTypeName = selectedField.Type.GetTypeElement()?.GetClrName();
                    Assertion.AssertNotNull(fieldTypeName);
                    var authoringFieldName = selectedField.ShortName;
                    var componentShortName = componentToAuthoringFieldNames[authoringFieldName];

                    var selectedFieldModule = selectedField.Module;
                    var authoringFieldType = TypeFactory.CreateTypeByCLRName(fieldTypeName, NullableAnnotation.NotAnnotated, selectedFieldModule);

                    var initializationFormat = "$0.$1";
                    var convertAuthoringToComponentField = BakerGeneratorUtils.ConvertAuthoringToComponentField(authoringFieldType.GetClrName(), selectedFieldModule);
                    
                    if(convertAuthoringToComponentField.HasValue)
                        initializationFormat = convertAuthoringToComponentField.Value.FunctionTemplate;
                
                    creationExpressionInitializer.AddMemberInitializerBefore(generationInfo.Factory.CreateObjectPropertyInitializer(
                        componentShortName,
                        generationInfo.Factory.CreateExpression(initializationFormat, authoringParameterName, authoringFieldName)), null);
                }

                componentCreationExpression.RemoveArgumentList();
            }
            
            componentCreationExpression.FormatNode(CodeFormatProfile.COMPACT);
        }

        private static IClassLikeDeclaration[] CreateBakerClassDeclaration(BakerGenerationInfo generationInfo)
        {
            // get parent class 'bakerTypeWithSubstitution' : Baker<ComponentNameAuthoring>
            var bakerGenericBaseClass = TypeFactory.CreateTypeByCLRName(KnownTypes.Baker, NullableAnnotation.NotAnnotated, generationInfo.Module);
            var bakerTypeElement = bakerGenericBaseClass.GetTypeElement().NotNull();
            var substitution = EmptySubstitution.INSTANCE.Extend(bakerTypeElement.TypeParameters[0], generationInfo.DeclaredAuthoringType);
            var bakerTypeWithSubstitution = TypeFactory.CreateType(bakerTypeElement, substitution, NullableAnnotation.NotAnnotated);

            //Create class 'ComponentDataBaker : Baker<ComponentNameAuthoring>'
            
            var (existingBakerTypeElement, _) = TypeFactory.CreateTypeByCLRName(generationInfo.BakerFullName, generationInfo.Module);

            IClassLikeDeclaration[] bakerClassDeclarations;
            //Must be derived from bakerTypeWithSubstitution
            if (existingBakerTypeElement != null && existingBakerTypeElement.IsDescendantOf(bakerTypeWithSubstitution.GetTypeElement()))
            {
                bakerClassDeclarations = existingBakerTypeElement.GetDeclarations().OfType<IClassLikeDeclaration>().ToArray();
                Assertion.Require(bakerClassDeclarations.Length > 0);
                return bakerClassDeclarations ;
            }

            bakerClassDeclarations = new[]
            {
                (IClassDeclaration)generationInfo.Factory
                    .CreateTypeMemberDeclaration("public class $0 : $1 { }", generationInfo.BakerUniqueClassName,
                        bakerTypeWithSubstitution)
            };

            using (WriteLockCookie.Create())
            {
                var bakerClassDeclaration = bakerClassDeclarations[0];
                bakerClassDeclaration = generationInfo.InsertionHelper.Insert(bakerClassDeclaration);
                bakerClassDeclaration.FormatNode(CodeFormatProfile.COMPACT);
                bakerClassDeclarations[0] = bakerClassDeclaration;
            }

            return bakerClassDeclarations;
        }

        private static IMethodDeclaration GetOrCreateBakeMethodExpression(IClassLikeDeclaration[] bakerClassDeclarations,
            CSharpElementFactory factory,
            BakerGenerationInfo generationInfo, out string authoringParameterName)
        {
            //'public override void Bake(ComponentNameAuthoring authoring)'
            const string bakeMethodName = "Bake";
            authoringParameterName = "authoring";

            //TODO: maybe check if implements void Baker<T>::Bake(T) 
            foreach (var bakerClassDeclaration in bakerClassDeclarations)
            {
                var existingBakeMethodDeclaration =
                    bakerClassDeclaration.MethodDeclarations.FirstOrDefault(m => m.DeclaredName.Equals(bakeMethodName));
                if (existingBakeMethodDeclaration is { IsOverride: true } &&
                    existingBakeMethodDeclaration.Type.IsVoid())
                {
                    var parameters = existingBakeMethodDeclaration.DeclaredElement.NotNull().Parameters;
                    if (parameters.Count == 1 &&
                        (parameters[0].Type.GetTypeElement()?.Equals(generationInfo.DeclaredAuthoringType.GetTypeElement()) ?? false))
                    {
                        authoringParameterName = parameters[0].ShortName;
                        return existingBakeMethodDeclaration;
                    }
                }
            }
            
            var bakeMethodExpression =
                (IMethodDeclaration)factory.CreateTypeMemberDeclaration("void $0($1 $2) {}", bakeMethodName, generationInfo.DeclaredAuthoringType,
                    authoringParameterName);
            bakeMethodExpression.SetOverride(true);
            bakeMethodExpression.SetAccessRights(AccessRights.PUBLIC);
            bakeMethodExpression = bakerClassDeclarations[0].AddClassMemberDeclaration(bakeMethodExpression);
            return bakeMethodExpression;
        }

        private static IObjectCreationExpression GetOrCreateComponentCreationExpression(CSharpElementFactory factory,
            IMethodDeclaration bakeMethodExpression, ITypeElement componentDeclaredType)
        {
            var existingCreationExpression = bakeMethodExpression.Body.FindNextNode( node =>
            {
                if (node is IMethodDeclaration)
                    return TreeNodeActionType.IGNORE_SUBTREE;
                
                return (node is IObjectCreationExpression expression
                        && componentDeclaredType.Equals(expression.Type().GetTypeElement()))
                    ? TreeNodeActionType.ACCEPT
                    : TreeNodeActionType.CONTINUE;
            });

            if (existingCreationExpression != null)
                return (IObjectCreationExpression)existingCreationExpression;

            //AddComponent(new ComponentData{})
            var addComponentStatement =
                (IExpressionStatement)bakeMethodExpression.Body.AddStatementAfter(factory.CreateStatement("AddComponent();"),
                    null);
            var addComponentExpression = (addComponentStatement.Expression as IInvocationExpression).NotNull();
            var creationArgument = addComponentExpression.AddArgumentAfter(
                factory.CreateArgument(ParameterKind.VALUE, factory.CreateExpression("new $0()", componentDeclaredType)), null);

            var componentCreationExpression = creationArgument.Value as IObjectCreationExpression;
            return componentCreationExpression!;
        }

        private static IObjectInitializer GetOrCreateInitializer(IObjectCreationExpression objectCreationExpression, CSharpElementFactory elementFactory)
        {
            var initializer = objectCreationExpression.Initializer;
            if (initializer is IObjectInitializer objectInitializer) 
                return objectInitializer;

            return (IObjectInitializer)objectCreationExpression.SetInitializer(elementFactory.CreateObjectInitializer());
        }

        private static ComponentDataGenerationResult GenerateComponentDataDeclaration(
            IGeneratorContext context,
            ComponentDataGenerationInfo componentDataGenerationInfo,
            ref Dictionary<string, string> authoringToComponentFieldNames)
        {
            var componentDataDeclaration = GetOrCreateComponentDataStructDeclaration(context, componentDataGenerationInfo);

            var selectedGeneratorElements = context.InputElements.OfType<GeneratorDeclaredElement>();
            var existingFields =  componentDataDeclaration.DeclaredElement.NotNull().Fields.ToDictionary(f => f.ShortName, f => f);
            foreach (var generatorElement in selectedGeneratorElements)
            {
                var declaredElement = generatorElement.DeclaredElement;
                if (declaredElement is not IField && declaredElement is not IProperty) 
                    continue;

                var selectedField = declaredElement as ITypeOwner;

                var authoringFieldShortName = selectedField!.ShortName;
                var componentFieldType = GetFieldType(selectedField);
                Assertion.AssertNotNull(componentFieldType);

                if (existingFields.TryGetValue(authoringFieldShortName, out var existingField))
                {
                    //Same field with same type
                    if (existingField.Type.Equals(componentFieldType))
                    {
                        authoringToComponentFieldNames.Add(authoringFieldShortName, authoringFieldShortName);
                        continue;
                    }
                    else
                    {
                        // TODO - for further refactoring feature: replace, delete, etc.
                    }
                }
                
                //Add field to Authoring class
                var componentFieldName = NamingUtil.GetUniqueName(componentDataDeclaration.Body, authoringFieldShortName, NamedElementKinds.PublicFields, null,
                    element => existingFields.ContainsKey(element.ShortName));
                authoringToComponentFieldNames.Add(authoringFieldShortName, componentFieldName);
                
                var fieldDeclaration = componentDataGenerationInfo.Factory.CreateFieldDeclaration(componentFieldType, componentFieldName);
                fieldDeclaration.SetAccessRights(AccessRights.PUBLIC);
                componentDataDeclaration.AddClassMemberDeclaration(fieldDeclaration);
            }

            var authoringType = componentDataGenerationInfo.AuthoringDeclaration.DeclaredElement;
            return new ComponentDataGenerationResult(TypeFactory.CreateType(authoringType!), componentDataGenerationInfo.AuthoringDeclaration, componentDataDeclaration);
        }

        private static IClassLikeDeclaration GetOrCreateComponentDataStructDeclaration(IGeneratorContext context, 
            ComponentDataGenerationInfo componentDataGenerationInfo)
        {
            // public class ComponentNameAuthoring : MonoBehaviour {}

            if (componentDataGenerationInfo.ExistingComponentData != null)
            {
                return (componentDataGenerationInfo.ExistingComponentData.GetDeclarations().SingleItem() as IClassLikeDeclaration)!;
            }

            var componentDataDeclaration = componentDataGenerationInfo.Factory.CreateTypeMemberDeclaration("public struct $0 : $1{}", componentDataGenerationInfo.NewComponentDataUniqueName,
                TypeFactory.CreateTypeByCLRName(KnownTypes.IComponentData, NullableAnnotation.NotAnnotated,
                    context.PsiModule)) as IClassLikeDeclaration;
            Assertion.AssertNotNull(componentDataDeclaration);

            return componentDataGenerationInfo.InsertionHelper.Insert(componentDataDeclaration);
        }

        private static IType GetFieldType(ITypeOwner selectedField)
        {
            var fieldTypeName = selectedField.Type.GetTypeElement().NotNull().GetClrName();
            var selectedFieldModule = selectedField.Module;
            var authoringFieldType = TypeFactory.CreateTypeByCLRName(fieldTypeName, NullableAnnotation.NotAnnotated, selectedFieldModule);
            var convertAuthoringToComponentField = BakerGeneratorUtils.ConvertAuthoringToComponentField(authoringFieldType.GetClrName(), selectedFieldModule);

            if (convertAuthoringToComponentField.HasValue)
                return TypeFactory.CreateTypeByCLRName(convertAuthoringToComponentField.Value.TypeName, NullableAnnotation.NotAnnotated, selectedFieldModule);
            
            return authoringFieldType;
        }

        private static bool HasUnityBaseType(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.DeclaredElement is IClass typeElement && UnityApi.IsDerivesFromComponent(typeElement);
        }

        private readonly struct BakerGenerationInfo
        {
            public readonly ITypeElement? ExistedBaker;
            public readonly IClassLikeDeclaration ComponentDataDeclaration;
            public readonly CSharpElementFactory Factory;
            public readonly IBakerInsertionHelper InsertionHelper;
            public readonly string BakerFullName;
            public readonly string BakerUniqueClassName;
            public readonly IDeclaredType DeclaredAuthoringType;
            public readonly IPsiModule Module;

            public BakerGenerationInfo(ITypeElement? existedBaker,
                ComponentDataGenerationResult componentDataGenerationResult,
                CSharpElementFactory factory, 
                IPsiModule module)
            {
                ExistedBaker = existedBaker;
                ComponentDataDeclaration = componentDataGenerationResult.ComponentDataDeclaration;
                Factory = factory;
                Module = module;
                InsertionHelper =  new NestedBakerInsertion(componentDataGenerationResult);
               
                if (ExistedBaker != null)
                {
                    BakerFullName = ExistedBaker.ShortName;
                    BakerUniqueClassName = BakerFullName;
                }
                else 
                {
                    var componentName = componentDataGenerationResult.AuthoringType.GetClrName().ShortName;
                    var bakerClassName = $"{componentName}Baker";

                    BakerFullName = $"{componentDataGenerationResult.ComponentDataDeclaration.DeclaredName}+{bakerClassName}";
                    BakerUniqueClassName = NamingUtil.GetUniqueName(componentDataGenerationResult.ComponentDataDeclaration, bakerClassName, NamedElementKinds.TypesAndNamespaces);

                }
               
                DeclaredAuthoringType = componentDataGenerationResult.AuthoringType;
            }
        }

        private interface IBakerInsertionHelper
        {
            IClassLikeDeclaration Insert(IClassLikeDeclaration bakerDeclaration);
        }

        private class NestedBakerInsertion : IBakerInsertionHelper
        {
            private readonly ComponentDataGenerationResult myComponentDataGenerationResult;

            public NestedBakerInsertion(ComponentDataGenerationResult componentDataGenerationResult)
            {
                myComponentDataGenerationResult = componentDataGenerationResult;
            }

            public IClassLikeDeclaration Insert(IClassLikeDeclaration bakerDeclaration)
            {
                return myComponentDataGenerationResult.AuthoringDeclaration.AddClassMemberDeclaration(bakerDeclaration);
            }
        }

        private readonly struct ComponentDataGenerationInfo
        {
            public readonly ITypeElement? ExistingComponentData;

            public readonly IClassLikeDeclaration AuthoringDeclaration;

            public readonly ComponentDataInsertionHelper InsertionHelper;
            
            public readonly string NewComponentDataUniqueName;
            public readonly CSharpElementFactory Factory;

            public ComponentDataGenerationInfo(ITypeElement? existingComponentData, IClassLikeDeclaration authoringDeclaration, CSharpElementFactory factory)
            {
                ExistingComponentData = existingComponentData;
                AuthoringDeclaration = authoringDeclaration;
                InsertionHelper = new ComponentDataInsertionHelper(authoringDeclaration);
                
                var baseName = $"{authoringDeclaration.DeclaredName.RemoveEnd("Authoring", StringComparison.OrdinalIgnoreCase)}ComponentData";
                
                NewComponentDataUniqueName = existingComponentData == null
                    ? NamingUtil.GetUniqueName(authoringDeclaration, baseName, NamedElementKinds.TypesAndNamespaces)
                    : string.Empty;
                Factory = factory;
            }
        }

        private readonly struct ComponentDataGenerationResult
        {
            public readonly IDeclaredType AuthoringType;
            public readonly IClassLikeDeclaration AuthoringDeclaration;
            public readonly IClassLikeDeclaration ComponentDataDeclaration;

            public ComponentDataGenerationResult(IDeclaredType authoringType, IClassLikeDeclaration authoringDeclaration, IClassLikeDeclaration componentDataDeclaration)
            {
                AuthoringType = authoringType;
                AuthoringDeclaration = authoringDeclaration;
                ComponentDataDeclaration = componentDataDeclaration;
            }
        }
        
        private class ComponentDataInsertionHelper
        {
            private readonly IClassLikeDeclaration myAuthoringDeclaration;

            public ComponentDataInsertionHelper(IClassLikeDeclaration authoringDeclaration)
            {
                myAuthoringDeclaration = authoringDeclaration;
            }

            public IClassLikeDeclaration Insert(IClassLikeDeclaration componentDataDeclaration)
            {
                using (WriteLockCookie.Create())
                    return ModificationUtil.AddChildAfter(myAuthoringDeclaration, componentDataDeclaration);
            }
        }
    }
}