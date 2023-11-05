#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
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
            var isDotsImplicitlyUsedType = typeElement.DerivesFrom(KnownTypes.ISystem);
            if (!isDotsImplicitlyUsedType)
                return;

            //possibly costly call: n*m calls - n - classes declarations, m - number of all methods
            var currentElementVisitor = new VisitorDotsMethods();
            ProcessMethods(element.MethodDeclarations, currentElementVisitor);

            var otherDeclarationsVisitor = new VisitorDotsMethods();
            var otherMethodDeclarations =
                DotsUtils.GetMethodsFromAllDeclarations(typeElement, declaration => declaration != element);
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

            void ProcessMethods(IEnumerable<IMethodDeclaration> treeNodeCollection,
                IRecursiveElementProcessor visitorDotsMethods)
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

                var hasGetTypeMethods = false;
                foreach (var singletonType in TryGetTypeFromGetSingletonCall(expression))
                {
                    GetSingletonCalls.Add(singletonType);
                    GetSingletonCallsExpressions.Add(singletonType, expression);
                    hasGetTypeMethods = true;
                }

                if (hasGetTypeMethods)
                    return; //this expression already has GetSingleton method, so 'RequiredForUpdate()' - not

                foreach (var singletonType in TryGetTypeFromRequireForUpdateCall(expression))
                    RequireForUpdateCalls.Add(singletonType);
            }

            private static IEnumerable<IType> TryGetTypeFromGetSingletonCall(IInvocationExpression expression)
            {
                var resolveResultWithInfo = expression.Reference.Resolve();
                var method = resolveResultWithInfo.DeclaredElement as IMethod;

                if (method == null)
                    return EmptyList<IType>.Instance;

                if (method.ShortName != "GetSingleton" && method.ShortName != "GetSingletonEntity")
                    return EmptyList<IType>.Instance;

                if (!method.ContainingType.IsClrName(KnownTypes.SystemAPI))
                    return EmptyList<IType>.Instance;

                var methodTypeParameters = method.TypeParameters;
                if (methodTypeParameters.Count != 1)
                    return EmptyList<IType>.Instance;

                return new List<IType> { resolveResultWithInfo.Substitution[methodTypeParameters[0]] };
            }

            private static IEnumerable<IType> TryGetTypeFromRequireForUpdateCall(IInvocationExpression expression)
            {
                var resolveResultWithInfo = expression.Reference.Resolve();

                if (resolveResultWithInfo.DeclaredElement is not IMethod method)
                    return EmptyList<IType>.Instance;

                if (method.ShortName == "RequireForUpdate" && method.ContainingType.IsClrName(KnownTypes.SystemState))
                {
                    var methodTypeParameters = method.TypeParameters;
                    if (methodTypeParameters.Count == 1)
                        return new List<IType> { resolveResultWithInfo.Substitution[methodTypeParameters[0]] };
                    
                    return EmptyList<IType>.Instance;

                }

                if (method.ShortName is "WithAll" or "WithAllRW" &&
                    method.ContainingType.IsClrName(KnownTypes.SystemAPIQueryBuilder))
                {
                    var methodTypeParameters = method.TypeParameters;
                    if (methodTypeParameters.Count < 1)
                        return EmptyList<IType>.Instance;

                    return methodTypeParameters.Select(p => resolveResultWithInfo.Substitution[p]);
                }

                return EmptyList<IType>.Instance;
            }

            public void ProcessAfterInterior(ITreeNode element)
            {
            }

            public bool ProcessingIsFinished => false;
        }
    }
}