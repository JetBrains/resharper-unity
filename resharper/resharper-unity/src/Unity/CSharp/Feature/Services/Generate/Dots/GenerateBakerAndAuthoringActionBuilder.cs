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
        
           private static ITreeNode GetOrCreateGetEntityExpression(ICSharpFunctionDeclaration bakeMethodExpression, CSharpElementFactory factory)
        {
            var entityNode = TryGetExistingEntityCreationNode(bakeMethodExpression);
            if (entityNode != null) 
                return entityNode;
            
            //return any AddComponent(...)
            var anyAddComponentMethodExpression = bakeMethodExpression.Body.FindNextNode(node =>
            {
                if (node is IMethodDeclaration)
                    return TreeNodeActionType.IGNORE_SUBTREE;

                if (node is not IInvocationExpression invocationExpression)
                    return TreeNodeActionType.CONTINUE;
                
                return invocationExpression.IsAnyAddComponentMethod()
                    ? TreeNodeActionType.ACCEPT
                    : TreeNodeActionType.CONTINUE;
            });


            if (anyAddComponentMethodExpression is IInvocationExpression { Arguments: { Count: > 0 } } methodExpression)
            {
                return methodExpression.Arguments[0].Value;
            }

            //var entity = GetEntity(TransformUsageFlags.Dynamic);
            var getEntityExpression =
                (IDeclarationStatement)bakeMethodExpression.Body.AddStatementAfter(factory.CreateStatement("var entity = GetEntity(TransformUsageFlags.Dynamic);"),
                    null);
            
            //returns "entity" node
            
            return getEntityExpression.VariableDeclarations.SingleItem!.FirstChild!;
        }

        private static ITreeNode? TryGetExistingEntityCreationNode(ICSharpFunctionDeclaration bakeMethodExpression)
        {
            var existingExpression = bakeMethodExpression.Body.FindNextNode(node =>
            {
                if (node is IMethodDeclaration)
                    return TreeNodeActionType.IGNORE_SUBTREE;

                if (node is not IInvocationExpression invocationExpression)
                    return TreeNodeActionType.CONTINUE;

                var localVariableDeclaration = invocationExpression.GetContainingNode<ILocalVariableDeclaration>();

                if (localVariableDeclaration == null)
                    return TreeNodeActionType.CONTINUE;

                return invocationExpression.IsBakerGetPrimaryEntityMethod()
                    ? TreeNodeActionType.ACCEPT
                    : TreeNodeActionType.CONTINUE;
            });

            var variableDeclaration = existingExpression?.GetContainingNode<ILocalVariableDeclaration>();

            var variableNameNode = variableDeclaration?.DeclaredElement.GetDeclarations().SingleItem()?.FirstChild;

            return variableNameNode;
        }

        private static void GenerateBaker(IGeneratorContext context, Dictionary<string, string> componentToAuthoringFieldNames, BakerGenerationInfo generationInfo)
        {
            var bakerClassDeclarations = generationInfo.ExistedBaker != null 
                ? generationInfo.ExistedBaker.GetDeclarations().OfType<IClassLikeDeclaration>().ToArray()
                : CreateBakerClassDeclaration(generationInfo);
            
            var bakeMethodExpression = GetOrCreateBakeMethodExpression(bakerClassDeclarations, generationInfo.Factory, generationInfo, out var authoringParameterName);
            var entityExpression = GetOrCreateGetEntityExpression(bakeMethodExpression, generationInfo.Factory);
            var isEmptyComponent = context.InputElements.Count == 0;
            if (isEmptyComponent)
            {
                CreateEmptyAddComponentExpression(generationInfo.Factory, bakeMethodExpression,
                    generationInfo.ComponentStructDeclaration.DeclaredElement!, entityExpression);
            }
            else
            {
                var componentCreationExpression = GetOrCreateComponentCreationExpression(generationInfo.Factory, bakeMethodExpression, generationInfo.ComponentStructDeclaration.DeclaredElement!, entityExpression);

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
                    var convertAuthoringToComponentField = ComponentToAuthoringConverter.Convert(authoringFieldType.GetClrName(), selectedField.Module);
                    if(convertAuthoringToComponentField.HasValue)
                        initializationFormat = convertAuthoringToComponentField.Value.FunctionTemplate;
                
                    creationExpressionInitializer.AddMemberInitializerBefore(generationInfo.Factory.CreateObjectPropertyInitializer(
                        fieldShortName,
                        generationInfo.Factory.CreateExpression(initializationFormat, authoringParameterName, authoringFieldName)), null);
                }

                componentCreationExpression.RemoveArgumentList();
                
                componentCreationExpression.FormatNode(CodeFormatProfile.COMPACT);
            }
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

        private static TreeNodeActionType FindIBakerAddComponentExpression(ITreeNode node,
            ITypeElement componentDeclaredType)
        {
            if (node is IMethodDeclaration)
                return TreeNodeActionType.IGNORE_SUBTREE;

            if (node is not IInvocationExpression invocationExpression)
                return TreeNodeActionType.CONTINUE;

            if (!invocationExpression.IsIBakerAddComponentMethod())
                return TreeNodeActionType.CONTINUE;

            var arguments = invocationExpression.ArgumentList.Arguments;
            if (arguments.Count == 0)
                return TreeNodeActionType.CONTINUE;

            var firstArgumentName = arguments[0].MatchingParameter.Element.ShortName;

            if (!firstArgumentName.Equals("entity"))
                return TreeNodeActionType.CONTINUE;

            var typeArguments = invocationExpression.TypeArguments;

            //if AddComponent(entity, new Component(){})
            if (typeArguments.Count == 0)
            {
                if (arguments.Count != 2)
                    return TreeNodeActionType.CONTINUE;

                var matchingParameterType = arguments[1].MatchingParameter.Type.GetTypeElement();
                return componentDeclaredType.Equals(matchingParameterType)
                    ? TreeNodeActionType.ACCEPT
                    : TreeNodeActionType.CONTINUE;
            }                
                
            
            //check if AddComponent<Component>(entity)
            if (typeArguments.Count == 1 && componentDeclaredType.Equals(typeArguments[0].GetTypeElement()))
                    return TreeNodeActionType.ACCEPT;
            
            return TreeNodeActionType.CONTINUE;
        }

        private static TreeNodeActionType FindIBakerAddComponentObjectExpression(ITreeNode node,
            ITypeElement componentDeclaredType)
        {
            
            if (node is IMethodDeclaration)
                return TreeNodeActionType.IGNORE_SUBTREE;

            if (node is not IInvocationExpression invocationExpression)
                return TreeNodeActionType.CONTINUE;

            if (!invocationExpression.IsIBakerAddComponentObjectMethod())
                return TreeNodeActionType.CONTINUE;
            
            //TODO check for number of params
            var arguments = invocationExpression.ArgumentList.Arguments;
            if (arguments.Count == 0)
                return TreeNodeActionType.CONTINUE;

            var firstArgumentName = arguments[0].MatchingParameter.Element.ShortName;

            if (!firstArgumentName.Equals("entity"))
                return TreeNodeActionType.CONTINUE;

            //if AddComponentObject(entity, new Component(){})
            if (arguments.Count != 2)
                return TreeNodeActionType.CONTINUE;

            var matchingParameterType = arguments[1].MatchingParameter.Type.GetTypeElement();
            return componentDeclaredType.Equals(matchingParameterType)
                ? TreeNodeActionType.ACCEPT
                : TreeNodeActionType.CONTINUE;

        }
        
         private static void CreateEmptyAddComponentExpression(CSharpElementFactory factory,
            IMethodDeclaration bakeMethodExpression, ITypeElement componentDeclaredType, ITreeNode entityExpression)
        {
            var isStructComponent = componentDeclaredType is IStruct;

            var existingAddComponentExpression = bakeMethodExpression.Body.FindNextNode(node => isStructComponent
                ? FindIBakerAddComponentExpression(node, componentDeclaredType)
                : FindIBakerAddComponentObjectExpression(node, componentDeclaredType));

            if (existingAddComponentExpression != null)
                return;

            //AddComponent/AddComponentObject(new ComponentData{})
            IExpressionStatement addComponentStatement;
            if (isStructComponent)
            {
                addComponentStatement = (IExpressionStatement)bakeMethodExpression.Body.AddStatementBefore(
                    factory.CreateStatement("AddComponent<$0>();", componentDeclaredType),
                    null);
            }
            else
            {
                addComponentStatement = (IExpressionStatement)bakeMethodExpression.Body.AddStatementBefore(
                    factory.CreateStatement("AddComponentObject();"),
                    null);
            }
            
            var addComponentExpression = (addComponentStatement.Expression as IInvocationExpression).NotNull();

            var entityArgument =
                factory.CreateArgument(ParameterKind.VALUE, factory.CreateExpression("$0", entityExpression));
            entityArgument = addComponentExpression.AddArgumentAfter(entityArgument, null);

            if (!isStructComponent)
            {
                addComponentExpression.AddArgumentAfter(
                    factory.CreateArgument(ParameterKind.VALUE,
                        factory.CreateExpression("new $0()", componentDeclaredType)), entityArgument);
            }
        }

        private static IObjectCreationExpression GetOrCreateComponentCreationExpression(CSharpElementFactory factory,
            IMethodDeclaration bakeMethodExpression, ITypeElement componentDeclaredType, ITreeNode entityExpression)
        {
            var isStructComponent = componentDeclaredType is IStruct;

            if (isStructComponent
                && bakeMethodExpression.Body.FindNextNode(node =>
                        FindIBakerAddComponentExpression(node, componentDeclaredType)) 
                    is IInvocationExpression invocationExpression)
            {
                var statement = invocationExpression.GetContainingStatement();
                var containingNode = statement?.GetContainingNode<IBlock>();
                containingNode?.RemoveStatement(statement!);
            }
            else
            {
                var existingCreationExpression = bakeMethodExpression.Body.FindNextNode(node =>
                {
                    if (node is IMethodDeclaration)
                        return TreeNodeActionType.IGNORE_SUBTREE;

                    if (node is not IObjectCreationExpression objectCreationExpression)
                        return TreeNodeActionType.CONTINUE;
                
                    if(!componentDeclaredType.Equals(objectCreationExpression.Type().GetTypeElement()))
                        return TreeNodeActionType.CONTINUE;

                    var parentInvocationExpression = objectCreationExpression.GetContainingNode<IInvocationExpression>();

                    if (parentInvocationExpression == null)
                        return TreeNodeActionType.CONTINUE;

                    if (!parentInvocationExpression.IsIBakerAddComponentMethod() &&
                        !parentInvocationExpression.IsIBakerAddComponentObjectMethod()) 
                        return TreeNodeActionType.CONTINUE;
                
                    var arguments = parentInvocationExpression.ArgumentList.Arguments;
                    if (arguments.Count == 0)
                        return TreeNodeActionType.CONTINUE;

                    var firstArgumentName = arguments[0].MatchingParameter.Element.ShortName;

                    if (!firstArgumentName.Equals("entity"))
                        return TreeNodeActionType.CONTINUE;

                    //if AddComponentObject(entity, new Component(){})
                    if (arguments.Count != 2)
                        return TreeNodeActionType.CONTINUE;

                    var matchingParameterType = arguments[1].MatchingParameter.Type.GetTypeElement();
                    return componentDeclaredType.Equals(matchingParameterType)
                        ? TreeNodeActionType.ACCEPT
                        : TreeNodeActionType.CONTINUE;

                });

                if (existingCreationExpression != null)
                    return (IObjectCreationExpression)existingCreationExpression;
            }

            //AddComponent/AddComponentObject(new ComponentData{})
            var addComponentMethod = componentDeclaredType is IStruct ? 
                 "AddComponent();" :  "AddComponentObject();";
            var addComponentStatement =
                (IExpressionStatement)bakeMethodExpression.Body.AddStatementBefore(factory.CreateStatement(addComponentMethod),
                    null);
            var addComponentExpression = (addComponentStatement.Expression as IInvocationExpression).NotNull();
            
            var entityArgument = factory.CreateArgument(ParameterKind.VALUE, factory.CreateExpression("$0", entityExpression));
            entityArgument = addComponentExpression.AddArgumentAfter(entityArgument, null);
            
            var creationArgument = addComponentExpression.AddArgumentAfter(
                factory.CreateArgument(ParameterKind.VALUE, factory.CreateExpression("new $0()", componentDeclaredType)), entityArgument);

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
            var existingAuthoringFields =  authoringDeclaration.DeclaredElement.NotNull().Fields.ToDictionary(f => f.ShortName, f => f);
            foreach (var generatorElement in selectedGeneratorElements)
            {  
                if (generatorElement.DeclaredElement is not IField selectedField) 
                    continue;
                
                var fieldShortName = selectedField.ShortName;

                var authoringFieldName = fieldShortName;
                if (authoringFieldName.Equals("Value"))
                    authoringFieldName = selectedField.ContainingType?.ShortName ?? fieldShortName;
                
                var authoringFieldType = BakerGeneratorUtils.GetFieldType(selectedField, ComponentToAuthoringConverter.Convert);
                Assertion.AssertNotNull(authoringFieldType);

                if (existingAuthoringFields.TryGetValue(authoringFieldName, out var existingField))
                {
                    //Same field with same type
                    if (existingField.Type.Equals(authoringFieldType))
                    {
                        componentToAuthoringFieldNames.Add(fieldShortName, existingField.ShortName);
                        continue;
                    }
                    else
                    {
                        // TODO - for further refactoring feature: replace, delete, etc.
                    }
                }
                
                //Add field to Authoring class
                authoringFieldName = NamingUtil.GetUniqueName(authoringDeclaration.Body, authoringFieldName, NamedElementKinds.PublicFields, null,
                    element => existingAuthoringFields.ContainsKey(element.ShortName));
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