#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.Analyzers
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration)
        , HighlightingTypes = new[] { typeof(SingletonMustBeRequestedWarning) }
    )]
    public class RequiredSingletonAnalyzer : UnityElementProblemAnalyzer<IClassLikeDeclaration>
    {
        public RequiredSingletonAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IClassLikeDeclaration element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var typeElement = element.DeclaredElement;
            var isDotsImplicitlyUsedType = UnityApi.IsDerivesFromISystem(typeElement);
            if (!isDotsImplicitlyUsedType)
                return;

            //possibly costly call: n*m calls - n - classes declarations, m - number of all methods
            var currentElementVisitor = new VisitorDotsMethods();
            ProcessMethods(element.MethodDeclarations, currentElementVisitor);

            var otherDeclarationsVisitor = new VisitorDotsMethods();
            var otherMethodDeclarations = DotsUtils.GetMethodsFromAllDeclarations(typeElement, declaration => declaration != element);
            ProcessMethods(otherMethodDeclarations, otherDeclarationsVisitor);

            currentElementVisitor.RequireForUpdateCalls.AddRange(otherDeclarationsVisitor.RequireForUpdateCalls);

            currentElementVisitor.GetSingletonCalls.ExceptWith(currentElementVisitor.RequireForUpdateCalls);

            foreach (var singletonCall in currentElementVisitor.GetSingletonCalls)
            {
                var singletonCallsExpressions = currentElementVisitor.GetSingletonCallsExpressions[singletonCall];
                foreach (var expression in singletonCallsExpressions)
                {
                    consumer.AddHighlighting(new SingletonMustBeRequestedWarning(element, expression, singletonCall));
                }
            }

            void ProcessMethods(IEnumerable<IMethodDeclaration> treeNodeCollection, IRecursiveElementProcessor visitorDotsMethods)
            {
                foreach (var methodDeclaration in treeNodeCollection)
                {
                    var method = methodDeclaration.DeclaredElement;
                    if (DotsUtils.IsISystemOnDestroyMethod(method))
                        continue;
                    methodDeclaration.ProcessThisAndDescendants(visitorDotsMethods);
                }
            }
        }

        private class VisitorDotsMethods : IRecursiveElementProcessor
        {
            public readonly HashSet<IType> RequireForUpdateCalls = new();
            public readonly HashSet<IType> GetSingletonCalls = new();
            public readonly OneToListMap<IType, IInvocationExpression> GetSingletonCallsExpressions = new();

            public bool InteriorShouldBeProcessed(ITreeNode element)
            {
                if (element is ILocalFunctionDeclaration)
                    return false;
                if (element is ILambdaExpression)
                    return false;

                return true;
            }

            public void ProcessBeforeInterior(ITreeNode element)
            {
                if (element is not IInvocationExpression expression)
                    return;

                if (TryGetTypeFromGetSingletonCall(expression, out var singletonType))
                {
                    GetSingletonCalls.Add(singletonType);
                    GetSingletonCallsExpressions.Add(singletonType, expression);
                }
                else if (TryGetTypeFromRequireForUpdateCall(expression, out singletonType))
                {
                    RequireForUpdateCalls.Add(singletonType);
                }
            }

            private static bool TryGetTypeFromGetSingletonCall(IInvocationExpression expression,
                                                               [MaybeNullWhen(false)] out IType singletonType)
            {
                singletonType = null;
                var resolveResultWithInfo = expression.Reference.Resolve();
                var method = resolveResultWithInfo.DeclaredElement as IMethod;

                if (method == null)
                    return false;

                if (method.ShortName != "GetSingleton" && method.ShortName != "GetSingletonEntity")
                    return false;

                if (!UnityApi.IsSystemAPI(method.ContainingType))
                    return false;

                var methodTypeParameters = method.TypeParameters;
                if (methodTypeParameters.Count != 1)
                    return false;

                singletonType = resolveResultWithInfo.Substitution[methodTypeParameters[0]];
                return true;
            }

            private static bool TryGetTypeFromRequireForUpdateCall(IInvocationExpression expression,
                                                                   [MaybeNullWhen(false)] out IType singletonType)
            {
                singletonType = null;
                var resolveResultWithInfo = expression.Reference.Resolve();
                var method = resolveResultWithInfo.DeclaredElement as IMethod;

                if (method == null)
                    return false;

                if (method.ShortName != "RequireForUpdate")
                    return false;

                if (!UnityApi.IsSystemStateType(method.ContainingType))
                    return false;

                var methodTypeParameters = method.TypeParameters;
                if (methodTypeParameters.Count != 1)
                    return false;

                singletonType = resolveResultWithInfo.Substitution[methodTypeParameters[0]];
                return true;
            }


            public void ProcessAfterInterior(ITreeNode element)
            {
            }

            public bool ProcessingIsFinished => false;
        }
    }
}
