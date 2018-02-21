using System;
using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute),
        HighlightingTypes = new[]
        {
            typeof(InvalidStaticModifierWarning),
            typeof(InvalidReturnTypeWarning),
            typeof(InvalidTypeParametersWarning),
            typeof(InvalidParametersWarning)
        })]
    public class AttributedMethodSignatureProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        private static readonly Dictionary<IClrTypeName, Func<PredefinedType, MethodSignature>> ourAttributeLookups =
            new Dictionary<IClrTypeName, Func<PredefinedType, MethodSignature>>
            {
                {KnownTypes.InitializeOnLoadMethodAttribute, GetStaticVoidMethodSignature},
                {KnownTypes.RuntimeInitializeOnLoadMethodAttribute, GetStaticVoidMethodSignature},
                {KnownTypes.DidReloadScripts, GetStaticVoidMethodSignature},
                {KnownTypes.OnOpenAssetAttribute, GetOnOpeAssetMethodSignature},
                {KnownTypes.PostProcessSceneAttribute, GetStaticVoidMethodSignature},
                {KnownTypes.PostProcessBuildAttribute, GetPostProcessBuildMethodSignature}
            };

        private readonly IPredefinedTypeCache myPredefinedTypeCache;

        public AttributedMethodSignatureProblemAnalyzer(UnityApi unityApi, IPredefinedTypeCache predefinedTypeCache)
            : base(unityApi)
        {
            myPredefinedTypeCache = predefinedTypeCache;
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!(element.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            if (ourAttributeLookups.TryGetValue(attributeTypeElement.GetClrName(), out var func))
            {
                var methodDeclaration = MethodDeclarationNavigator.GetByAttribute(element);
                if (methodDeclaration == null)
                    return;

                var predefinedType = myPredefinedTypeCache.GetOrCreatePredefinedType(element.GetPsiModule());
                var methodSignature = func(predefinedType);

                if (!methodSignature.HasMatchingStaticModifier(methodDeclaration))
                    consumer.AddHighlighting(new InvalidStaticModifierWarning(methodDeclaration, methodSignature));
                if (!methodSignature.HasMatchingReturnType(methodDeclaration))
                    consumer.AddHighlighting(new InvalidReturnTypeWarning(methodDeclaration, methodSignature));
                if (!methodSignature.HasMatchingTypeParameters(methodDeclaration))
                    consumer.AddHighlighting(new InvalidTypeParametersWarning(methodDeclaration, methodSignature));
                if (!methodSignature.HasMatchingParameters(methodDeclaration))
                    consumer.AddHighlighting(new InvalidParametersWarning(methodDeclaration, methodSignature));
            }
        }

        private static MethodSignature GetStaticVoidMethodSignature(PredefinedType predefinedType)
        {
            return new MethodSignature(predefinedType.Void, true);
        }

        private static MethodSignature GetOnOpeAssetMethodSignature(PredefinedType predefinedType)
        {
            return new MethodSignature(predefinedType.Bool, true,
                new[] {predefinedType.Int, predefinedType.Int},
                new[] {"instanceID", "line"});
        }

        private static MethodSignature GetPostProcessBuildMethodSignature(PredefinedType predefinedType)
        {
            var buildTargetType = TypeFactory.CreateTypeByCLRName("UnityEditor.BuildTarget", predefinedType.Module);
            return new MethodSignature(predefinedType.Void, true,
                new[] {buildTargetType, predefinedType.String},
                new[] {"target", "pathToBuildProject"});
        }
    }
}