using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute),
        HighlightingTypes = new[]
        {
            typeof(InvalidStaticModifierWarning), typeof(InvalidReturnTypeWarning),
            typeof(InvalidTypeParametersWarning), typeof(InvalidParametersWarning)
        })]
    public class DrawGizmoAttributeProblemAnalyzer : MethodSignatureProblemAnalyzerBase<IAttribute>
    {
        private readonly IPredefinedTypeCache myPredefinedTypeCache;

        public DrawGizmoAttributeProblemAnalyzer([NotNull] UnityApi unityApi, IPredefinedTypeCache predefinedTypeCache)
            : base(unityApi)
        {
            myPredefinedTypeCache = predefinedTypeCache;
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (!(element.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            // Otherwise we'll treat it as targeting a method
            if (element.Target == AttributeTarget.Return)
                return;

            if (attributeTypeElement.GetClrName().Equals(KnownTypes.DrawGizmo))
            {
                var methodDeclaration = MethodDeclarationNavigator.GetByAttribute(element);
                CheckMethodDeclaration(methodDeclaration, element, consumer);
            }
        }

        private void CheckMethodDeclaration(IMethodDeclaration methodDeclaration, IAttribute element, IHighlightingConsumer consumer)
        {
            if (methodDeclaration == null)
                return;
            
            var predefinedType = myPredefinedTypeCache.GetOrCreatePredefinedType(element.GetPsiModule());
            var gizmoType = TypeFactory.CreateTypeByCLRName("UnityEditor.GizmoType", predefinedType.Module);
            var componentType = TypeFactory.CreateTypeByCLRName("UnityEngine.Component", predefinedType.Module);
          

            var expectedDeclaration = new MethodSignature(predefinedType.Void, true,
                new[] {componentType, gizmoType},
                new[] {"component", "gizmoType"});

            var match = expectedDeclaration.Match(methodDeclaration);
            
            if (methodDeclaration.Params.ParameterDeclarations.Count == 2)
            {
                var parameters = methodDeclaration.Params.ParameterDeclarations;

                Log.Root.Log(LoggingLevel.INFO, string.Format("component {0}:{1}:{2}", parameters[0].Type.GetTypeElement(), componentType.GetTypeElement(), parameters[0].Type.GetTypeElement()
                    ?.IsDescendantOf(componentType.GetTypeElement())));
                Log.Root.Log(LoggingLevel.INFO, string.Format("gizmo {0}:{1}:{2}", parameters[1].Type.GetTypeElement(), gizmoType.GetTypeElement(), parameters[1].Type.GetTypeElement()?.Equals(gizmoType?.GetTypeElement())));

                if (parameters[0].Type.GetTypeElement()
                        ?.IsDescendantOf(componentType.GetTypeElement()) == true)
                {
                    Log.Root.Log(LoggingLevel.INFO, "First matched");
                    if (parameters[1].Type.GetTypeElement()?.Equals(gizmoType?.GetTypeElement()) == true)
                    {
                        Log.Root.Log(LoggingLevel.INFO, "second matched");
                        match &= ~MethodSignatureMatch.IncorrectParameters;
                    }
                
                    expectedDeclaration = new MethodSignature(predefinedType.Void, true,
                        new[] {parameters[0].Type, gizmoType},
                        new[] {parameters[0].DeclaredName, parameters[1].DeclaredName});
                }
            }
            
            base.AddMethodSignatureInspections(consumer, methodDeclaration, expectedDeclaration, match);
        }
    }
}