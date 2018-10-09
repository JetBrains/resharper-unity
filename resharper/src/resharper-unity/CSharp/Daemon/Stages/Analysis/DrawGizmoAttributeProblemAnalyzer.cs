using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

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
            var gizmoType = TypeFactory.CreateTypeByCLRName(KnownTypes.GizmoType, predefinedType.Module);
            var componentType = TypeFactory.CreateTypeByCLRName(KnownTypes.Component, predefinedType.Module);

            IType derivedType = componentType;
            var derivedName = "component";
            var gizmoName = "gizmoType";
            var firstParamCorrect = false;
            var secondParamCorrect = false;

            for (var i = 0; i < methodDeclaration.Params.ParameterDeclarations.Count; i++)
            {
                var param = methodDeclaration.Params.ParameterDeclarations[i];
                if (param.Type.GetTypeElement()
                        ?.IsDescendantOf(componentType.GetTypeElement()) == true)
                {
                    if (i == 0)
                        firstParamCorrect = true;

                    derivedType = param.Type;
                    derivedName = param.DeclaredName;
                }

                if (param.Type.GetTypeElement()
                        ?.Equals(gizmoType?.GetTypeElement()) == true)
                {
                    if (i == 1)
                        secondParamCorrect = true;

                    gizmoName = param.DeclaredName;
                }
            }

            var expectedDeclaration = new MethodSignature(predefinedType.Void, true,
                new[] {derivedType, gizmoType},
                new[] {derivedName, gizmoName});
            var match = expectedDeclaration.Match(methodDeclaration);

            if (methodDeclaration.Params.ParameterDeclarations.Count == 2)
            {
                if (firstParamCorrect && secondParamCorrect)
                {
                    match &= ~MethodSignatureMatch.IncorrectParameters;
                }
                else if (!firstParamCorrect && secondParamCorrect && match == MethodSignatureMatch.IncorrectParameters)
                {
                    // TODO: Should this be ExpectedComponentWarning?
                    consumer.AddHighlighting(new ParameterNotDerivedFromComponentWarning(methodDeclaration.Params.ParameterDeclarations.First()));
                    return;
                }
            }

            AddMethodSignatureInspections(consumer, methodDeclaration, expectedDeclaration, match);
        }
    }
}