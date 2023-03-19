#nullable enable
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

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    [GeneratorBuilder(GeneratorUnityKinds.UnityGenerateBakerAndAuthoring, typeof(CSharpLanguage))]
    public class GenerateBakerAndAuthoringActionBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
    {
        public override double Priority => 100;

        private const string SelectedBaker = "SelectedBaker";

        private readonly Dictionary<string, ITypeElement> myExistedBakers = new(100);

        protected override void BuildOptions(CSharpGeneratorContext context, ICollection<IGeneratorOption> options)
        {
            base.BuildOptions(context, options);
            
            var (bakerBaseTypeElement, _) = TypeFactory.CreateTypeByCLRName(KnownTypes.Baker, NullableAnnotation.Unknown, context.PsiModule);
            var typeElements = new List<ITypeElement>();

            var solution = context.Solution;
            var packageManager = solution.GetComponent<PackageManager>();
            var finder = solution.GetPsiServices().Finder;
            finder.FindInheritors(bakerBaseTypeElement,  typeElements.ConsumeDeclaredElements(), NullProgressIndicator.Create());

            var availableBakers = new HashSet<string>
            {
                Strings.UnityDots_GenerateBakerAndAuthoring_NewBaker_As_Nested,
                Strings.UnityDots_GenerateBakerAndAuthoring_NewBaker
            };
            myExistedBakers.Clear();

            foreach (var typeElement in typeElements)
            {
                if (!typeElement.IsFromUnityProject()) 
                    continue;
                //skip bakers from packages
                var packageData =
                    packageManager.GetOwningPackage(typeElement.GetSingleOrDefaultSourceFile().GetLocation());
                if (packageData != null && packageData.Source != PackageSource.Local)
                    continue;
                
                    
                var name = typeElement.GetClrName().FullName;
                availableBakers.Add(name);
                myExistedBakers[name] = typeElement;
            }

            var selector = new GeneratorOptionSelector(SelectedBaker, Strings.UnityDots_GenerateBakerAndAuthoring_Baker, availableBakers.ToIReadOnlyList())
                { Value = Strings.UnityDots_GenerateBakerAndAuthoring_NewBaker_As_Nested };
            
            options.Add(selector);
        }

        // Enables/disables the menu item
        protected override bool IsAvailable(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.IsFromUnityProject() && IsInheritorOfComponentData(context) && base.IsAvailable(context);
        }

        // provides baker generation for empty Component
        protected override bool HasProcessableElements(CSharpGeneratorContext context, IEnumerable<IGeneratorElement> elements)
        {
            return true;
        }

        protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
        {
            if (!IsInheritorOfComponentData(context)) 
                return;
            
            var (selectedBaker, generateAsNested) = GetSelectedBaker(context);
            var selectedAuthoringComponent = GetSelectedAuthoringComponent(selectedBaker);

            var componentToAuthoringFieldNames = new Dictionary<string, string>(context.InputElements.Count);
            var componentStructDeclaration = context.ClassDeclaration;
            var factory = CSharpElementFactory.GetInstance(componentStructDeclaration);
            
            var authoringGenerationInfo = new AuthoringGenerationInfo(selectedAuthoringComponent, componentStructDeclaration, factory); 
            var authoringGenerationResult = GenerateAuthoringDeclaration(context, authoringGenerationInfo, ref componentToAuthoringFieldNames);
            
            var bakerGenerationInfo = new BakerGenerationInfo(selectedBaker, generateAsNested, authoringGenerationResult, componentStructDeclaration, factory, context.PsiModule);
            GenerateBaker(context, componentToAuthoringFieldNames, bakerGenerationInfo);
        }

        private static ITypeElement? GetSelectedAuthoringComponent(ITypeElement? selectedBaker)
        {
            if (selectedBaker == null)
                return null;
            foreach (var (typeElement, substitution) in selectedBaker.GetSuperTypes())
            {
                if (typeElement.IsClrName(KnownTypes.Baker))
                {
                    var authoringType = typeElement.TypeParameters[0];
                    var type = substitution[authoringType];
                    return type.GetTypeElement();
                }
            }

            return null;
        }

        private (ITypeElement?, bool) GetSelectedBaker(CSharpGeneratorContext context)
        {
            var selectedBaker = context.GetOption(SelectedBaker);

            if (string.IsNullOrEmpty(selectedBaker))
                return (null, true);
            
            if (myExistedBakers.TryGetValue(selectedBaker, out var baker))
            {
                return (baker, false);
            }

            var asNested = selectedBaker.Equals(Strings.UnityDots_GenerateBakerAndAuthoring_NewBaker_As_Nested);
            return (null, asNested);
        }

        private static void GenerateBaker(IGeneratorContext context, Dictionary<string, string> componentToAuthoringFieldNames, BakerGenerationInfo generationInfo)
        {
            var bakerClassDeclarations = generationInfo.ExistedBaker != null 
                ? generationInfo.ExistedBaker.GetDeclarations().OfType<IClassLikeDeclaration>().ToArray()
                : CreateBakerClassDeclaration(generationInfo);
            
            var bakeMethodExpression = GetOrCreateBakeMethodExpression(bakerClassDeclarations, generationInfo.Factory, generationInfo, out var authoringParameterName);
            var componentCreationExpression = GetOrCreateComponentCreationExpression(generationInfo.Factory, bakeMethodExpression, generationInfo.ComponentStructDeclaration.DeclaredElement!);
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
                    var fieldShortName = selectedField.ShortName;
                    var authoringFieldName = componentToAuthoringFieldNames[fieldShortName];
                
                    var authoringFieldType = TypeFactory.CreateTypeByCLRName(fieldTypeName, NullableAnnotation.NotAnnotated, selectedField.Module);

                    var initializationFormat = "$0.$1";
                    var convertAuthoringToComponentField = BakerGeneratorUtils.ConvertComponentToAuthoringField(authoringFieldType.GetClrName(), selectedField.Module);
                    if(convertAuthoringToComponentField.HasValue)
                        initializationFormat = convertAuthoringToComponentField.Value.FunctionTemplate;
                
                    creationExpressionInitializer.AddMemberInitializerBefore(generationInfo.Factory.CreateObjectPropertyInitializer(
                        fieldShortName,
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
            CSharpElementFactory factory, BakerGenerationInfo generationInfo, out string authoringParameterName)
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
                        (parameters[0].Type.GetTypeElement()
                            ?.Equals(generationInfo.DeclaredAuthoringType.GetTypeElement()) ?? false))
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

            //AddComponent/AddComponentObject(new ComponentData{})
            var addComponentMethod = componentDeclaredType is IStruct ? "AddComponent();" :  "AddComponentObject();";
            var addComponentStatement =
                (IExpressionStatement)bakeMethodExpression.Body.AddStatementAfter(factory.CreateStatement(addComponentMethod),
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

        private static AuthoringGenerationResult GenerateAuthoringDeclaration(
            CSharpGeneratorContext context, AuthoringGenerationInfo authoringGenerationInfo,
            ref Dictionary<string, string> componentToAuthoringFieldNames)
        {
            var authoringDeclaration = GetOrCreateAuthoringClassDeclaration(context, authoringGenerationInfo);

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
                    element => existingFields.ContainsKey(element.ShortName));
                componentToAuthoringFieldNames.Add(fieldShortName, authoringFieldName);
                
                var fieldDeclaration = authoringGenerationInfo.Factory.CreateFieldDeclaration(authoringFieldType, authoringFieldName);
                fieldDeclaration.SetAccessRights(AccessRights.PUBLIC);
                authoringDeclaration.AddClassMemberDeclaration(fieldDeclaration);
            }
            
            return new AuthoringGenerationResult(TypeFactory.CreateType(authoringDeclaration.DeclaredElement!), authoringDeclaration);
        }

        private static IClassLikeDeclaration GetOrCreateAuthoringClassDeclaration(IGeneratorContext context, AuthoringGenerationInfo authoringGenerationInfo)
        {
            // public class ComponentNameAuthoring : MonoBehaviour {}

            if (authoringGenerationInfo.ExistingAuthoring != null)
            {
                return (authoringGenerationInfo.ExistingAuthoring.GetDeclarations().FirstOrDefault() as IClassLikeDeclaration)!;
            }

            var authoringDeclaration = authoringGenerationInfo.Factory.CreateTypeMemberDeclaration("public class $0 : $1{}", authoringGenerationInfo.NewAuthoringUniqueName,
                TypeFactory.CreateTypeByCLRName(KnownTypes.MonoBehaviour, NullableAnnotation.NotAnnotated,
                    context.PsiModule)) as IClassDeclaration;
            Assertion.AssertNotNull(authoringDeclaration);

            return authoringGenerationInfo.InsertionHelper.Insert(authoringDeclaration);
        }

        private static IType GetFieldType(IField selectedField)
        {
            var fieldTypeName = selectedField.Type.GetTypeElement().NotNull().GetClrName();
            var selectedFieldModule = selectedField.Module;
            var authoringFieldType = TypeFactory.CreateTypeByCLRName(fieldTypeName, NullableAnnotation.NotAnnotated, selectedFieldModule);
            var convertAuthoringToComponentField = BakerGeneratorUtils.ConvertComponentToAuthoringField(authoringFieldType.GetClrName(), selectedFieldModule);

            if (convertAuthoringToComponentField.HasValue)
                return TypeFactory.CreateTypeByCLRName(convertAuthoringToComponentField.Value.TypeName, NullableAnnotation.NotAnnotated, selectedFieldModule);
            
            return authoringFieldType;
        }

        private static bool IsInheritorOfComponentData(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.DeclaredElement.DerivesFrom(KnownTypes.IComponentData);
        }

        private readonly struct BakerGenerationInfo
        {
            public readonly ITypeElement? ExistedBaker;
            public readonly IClassLikeDeclaration ComponentStructDeclaration;
            public readonly CSharpElementFactory Factory;
            public readonly IBakerInsertionHelper InsertionHelper;
            public readonly string BakerFullName;
            public readonly string BakerUniqueClassName;
            public readonly IDeclaredType DeclaredAuthoringType;
            public readonly IPsiModule Module;

            public BakerGenerationInfo(ITypeElement? existedBaker, bool generateAsNested, AuthoringGenerationResult authoringGenerationResult, IClassLikeDeclaration componentStructDeclaration,
                CSharpElementFactory factory, IPsiModule module)
            {
                ExistedBaker = existedBaker;
                ComponentStructDeclaration = componentStructDeclaration;
                Factory = factory;
                Module = module;
                InsertionHelper = generateAsNested
                    ? new NestedBakerInsertion(authoringGenerationResult)
                    : new NewBakerInsertion(authoringGenerationResult);
                
                var componentName = componentStructDeclaration.DeclaredName;
                var bakerClassName = $"{componentName}Baker";

                if (ExistedBaker != null)
                {
                    BakerFullName = ExistedBaker.GetClrName().FullName;
                    BakerUniqueClassName = BakerFullName;
                }
                else if (generateAsNested)
                {
                    BakerFullName = $"{authoringGenerationResult.AuthoringDeclaration.CLRName}+{bakerClassName}";
                    BakerUniqueClassName = NamingUtil.GetUniqueName(authoringGenerationResult.AuthoringDeclaration, bakerClassName, NamedElementKinds.TypesAndNamespaces);

                }
                else
                {
                    BakerUniqueClassName = NamingUtil.GetUniqueName(componentStructDeclaration, bakerClassName, NamedElementKinds.TypesAndNamespaces);
                    BakerFullName = $"{authoringGenerationResult.AuthoringDeclaration.CLRName}+{BakerUniqueClassName}";
                }
                
                DeclaredAuthoringType = authoringGenerationResult.AuthoringDeclaredType;
            }
        }

        private interface IBakerInsertionHelper
        {
            IClassLikeDeclaration Insert(IClassLikeDeclaration bakerDeclaration);
        }

        private class NestedBakerInsertion : IBakerInsertionHelper
        {
            private readonly AuthoringGenerationResult myAuthoringGenerationResult;

            public NestedBakerInsertion(AuthoringGenerationResult authoringGenerationResult)
            {
                myAuthoringGenerationResult = authoringGenerationResult;
            }

            public IClassLikeDeclaration Insert(IClassLikeDeclaration bakerDeclaration)
            {
                return myAuthoringGenerationResult.AuthoringDeclaration.AddClassMemberDeclaration(bakerDeclaration);
            }
        }

        private class NewBakerInsertion : IBakerInsertionHelper
        {
            private readonly AuthoringGenerationResult myAuthoringGenerationResult;

            public NewBakerInsertion(AuthoringGenerationResult authoringGenerationResult)
            {
                myAuthoringGenerationResult = authoringGenerationResult;
            }

            public IClassLikeDeclaration Insert(IClassLikeDeclaration bakerDeclaration)
            {
                return ModificationUtil.AddChildAfter(myAuthoringGenerationResult.AuthoringDeclaration, bakerDeclaration);
            }
        }

        private readonly struct AuthoringGenerationInfo
        {
            /*existingAuthoringTypeElement != null && existingAuthoringTypeElement.DerivesFrom(KnownTypes.MonoBehaviour)*/
            public readonly ITypeElement? ExistingAuthoring;
            
            public readonly AuthoringInsertionHelper InsertionHelper;
            
            public readonly string NewAuthoringUniqueName;
            public readonly CSharpElementFactory Factory;

            public AuthoringGenerationInfo(ITypeElement? existingAuthoring, IClassLikeDeclaration componentStructDeclaration, CSharpElementFactory factory)
            {
                ExistingAuthoring = existingAuthoring;
                InsertionHelper = new AuthoringInsertionHelper(componentStructDeclaration);
                NewAuthoringUniqueName = existingAuthoring == null
                    ? NamingUtil.GetUniqueName(componentStructDeclaration, $"{componentStructDeclaration.DeclaredName}Authoring", NamedElementKinds.TypesAndNamespaces)
                    : string.Empty;
                Factory = factory;
            }
        }

        private readonly struct AuthoringGenerationResult
        {
            public readonly IDeclaredType AuthoringDeclaredType;
            public readonly IClassLikeDeclaration AuthoringDeclaration;

            public AuthoringGenerationResult(IDeclaredType authoringDeclaredType, IClassLikeDeclaration authoringDeclaration)
            {
                AuthoringDeclaredType = authoringDeclaredType;
                AuthoringDeclaration = authoringDeclaration;
            }
        }
        
        private class AuthoringInsertionHelper
        {
            private readonly IClassLikeDeclaration myComponentStructDeclaration;

            public AuthoringInsertionHelper(IClassLikeDeclaration componentStructDeclaration)
            {
                myComponentStructDeclaration = componentStructDeclaration;
            }

            public IClassLikeDeclaration Insert(IClassLikeDeclaration authoringDeclaration)
            {
                using (WriteLockCookie.Create())
                    return ModificationUtil.AddChildAfter(myComponentStructDeclaration, authoringDeclaration);
            }
        }
    }
}