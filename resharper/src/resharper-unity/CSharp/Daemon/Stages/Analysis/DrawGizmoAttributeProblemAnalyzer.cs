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
            var gizmoTypeName = TypeFactory.CreateTypeByCLRName("UnityEditor.GizmoType", predefinedType.Module);
            var componentName = TypeFactory.CreateTypeByCLRName("UnityEngine.Component", predefinedType.Module);

            if (methodDeclaration.Params.ParameterDeclarations.Count == 2)
            {
                var parameters = methodDeclaration.Params.ParameterDeclarations;
                var expectedDeclaration = new MethodSignature(predefinedType.Void, true,
                    new[] {componentName, gizmoTypeName},
                    new[] {parameters[0].DeclaredName, parameters[1].DeclaredName});

                if (methodDeclaration.Type.IsVoid())
                    if (methodDeclaration.IsStatic)
                        if (parameters[0].Type.GetTypeElement()
                                ?.IsDescendantOf(componentName.GetTypeElement()) == true)
                        {
                            if (parameters[1].Type.GetTypeElement()
                                    ?.IsDescendantOf(gizmoTypeName.GetTypeElement()) == false)
                            {
                                expectedDeclaration = new MethodSignature(predefinedType.Void, true,
                                    new[] {parameters[0].Type, gizmoTypeName},
                                    new[] {parameters[0].DeclaredName, parameters[1].DeclaredName});
                                consumer.AddHighlighting(new IncorrectSignatureWarning(methodDeclaration,
                                    expectedDeclaration, MethodSignatureMatch.IncorrectParameters));
                            }
                        }
                        else
                            consumer.AddHighlighting(new IncorrectSignatureWarning(methodDeclaration,
                                expectedDeclaration, MethodSignatureMatch.IncorrectParameters));
                    else
                        consumer.AddHighlighting(
                            new InvalidStaticModifierWarning(methodDeclaration, expectedDeclaration));
                else
                    consumer.AddHighlighting(new InvalidReturnTypeWarning(methodDeclaration, expectedDeclaration));
            }
            else
            {
                var expectedDeclaration = new MethodSignature(predefinedType.Void, true,
                    new[] {componentName, gizmoTypeName},
                    new[] {"scr", "gizmoType"});
                consumer.AddHighlighting(new InvalidParametersWarning(methodDeclaration, expectedDeclaration));
            }
        }
    }
}