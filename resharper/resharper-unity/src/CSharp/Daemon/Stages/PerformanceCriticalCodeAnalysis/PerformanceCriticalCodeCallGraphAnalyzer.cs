using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [ShellComponent]
    public class PerformanceCriticalCodeCallGraphAnalyzer : ICallGraphElementAnalyzer
    {
        public const string MarkId = "PerformanceCriticalCode";

        public ICallGraphPropagator CreatePropagator(ISolution solution) =>
            new CallerToCallingCallGraphPropagator(solution, MarkId);

        public string GetMarkId() => MarkId;

        private static readonly ISet<string> ourKnownHotMonoBehaviourMethods = new HashSet<string>()
        {
            "Update", "LateUpdate", "FixedUpdate",
        };

        public IDeclaredElement Mark(ITreeNode currentNode)
        {
            if (currentNode is IMethodDeclaration methodDeclaration &&
                ourKnownHotMonoBehaviourMethods.Contains(methodDeclaration.DeclaredName))
            {
                var containingTypeDeclaration = methodDeclaration.GetContainingTypeDeclaration();
                if (containingTypeDeclaration == null)
                    return null;

                if (containingTypeDeclaration.SuperTypes.Any(t => t.GetClrName().Equals(KnownTypes.MonoBehaviour)))
                    return methodDeclaration.DeclaredElement;;

                return null;
            }

            if (currentNode is IInvocationExpression invocationExpression)
            {
                // we should find 'StartCoroutine' method, because passed symbol will be hot too
                var reference = (invocationExpression.InvokedExpression as IReferenceExpression)?.Reference;
                if (reference == null)
                    return null;

                var info = reference.Resolve();
                if (info.ResolveErrorType != ResolveErrorType.OK)
                    return null;

                var declaredElement = info.DeclaredElement as IMethod;
                if (declaredElement == null)
                    return null;

                var containingType = declaredElement.GetContainingType();

                if (containingType == null || containingType.GetClrName().Equals(KnownTypes.MonoBehaviour))
                {
                    if (!declaredElement.ShortName.Equals("StartCoroutine") &&
                        !declaredElement.ShortName.Equals("InvokeRepeating"))
                    {
                        return null;
                    }

                    var firstArgument = invocationExpression.Arguments.FirstOrDefault()?.Value;
                    if (firstArgument == null)
                        return null;

                    var coroutineMethodDeclaration = ExtractMethodDeclarationFromStartCoroutine(firstArgument);
                    return coroutineMethodDeclaration;
                }
            }

            return null;
        }
        
        private IDeclaredElement ExtractMethodDeclarationFromStartCoroutine([NotNull]ICSharpExpression firstArgument)
        {
            // 'StartCoroutine' has overload with string. We have already attached reference, so get declaration from
            // reference
            if (firstArgument is ILiteralExpression literalExpression)
            {
                var coroutineMethodReference = literalExpression.GetReferences<UnityEventFunctionReference>().FirstOrDefault();
                if (coroutineMethodReference != null)
                {
                    return coroutineMethodReference.Resolve().DeclaredElement;
                }
            }

            // argument is IEnumerator which is returned from invocation, so get invocation declaration
            if (firstArgument is IInvocationExpression coroutineInvocation)
            {
                var invocationReference = (coroutineInvocation.InvokedExpression as IReferenceExpression)?.Reference;
                var info = invocationReference?.Resolve();
                return info?.DeclaredElement;
            }

            return null;
        }
    }
}