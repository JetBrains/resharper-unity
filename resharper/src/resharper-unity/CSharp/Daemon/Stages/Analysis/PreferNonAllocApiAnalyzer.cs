using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes =
        new[] {typeof(PreferNonAllocApiWarning),})]
    public class PreferNonAllocApiAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        public PreferNonAllocApiAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (!(expression.InvokedExpression is IReferenceExpression referenceExpression)) return;

            var reference = expression.Reference;
            if (reference == null) return;

            if (HasNonAllocVersion(reference, out var nonAllocName))
            {
                consumer.AddHighlighting(new PreferNonAllocApiWarning(referenceExpression.Reference, nonAllocName));
            }
        }

        private bool HasNonAllocVersion(ICSharpInvocationReference reference, out string nonAllocName)
        {
            var info = reference.Resolve();
            nonAllocName = null;

            if (info.ResolveErrorType == ResolveErrorType.OK && info.DeclaredElement is IMethod method)
            {
                var originName = method.ShortName;
                if (originName.Length < 3) return false;
                
                var suffix = originName.Substring(originName.Length - 3, 3);
                var newName = (suffix.Equals("All") ? originName.Substring(0, originName.Length - 3) : originName) + "NonAlloc";

                // try to find method with same parameters + return value as parameter
                var candidates = method.GetContainingType()?.Methods
                    .Where(t => t.ShortName.Equals(newName) && MatchSignatureAllocToNonAlloc(method, t)).ToArray();

                if (candidates?.SingleOrDefault() != null)
                {
                    nonAllocName = newName;
                    return true;
                }

                return false;
            }

            return false;
        }

        private bool MatchSignatureAllocToNonAlloc(IMethod method, IMethod nonAllocMethod)
        {
            var originReturnType = method.ReturnType;
            var originParameters = method.Parameters;
            var nonAllocMethodParameters = nonAllocMethod.Parameters;
            var originSize = originParameters.Count;
            
            if (originSize + 1 != nonAllocMethodParameters.Count)
            {
                return false;
            }

            int curOriginParameterIdx = 0;

            foreach (var nonAllocParameter in nonAllocMethodParameters)
            {
                if (nonAllocParameter.Type.Equals(originReturnType))
                {
                    continue;
                }
                
                if (curOriginParameterIdx == originSize)
                {
                    return false;
                }   
                
                if (nonAllocParameter.Type.Equals(originParameters[curOriginParameterIdx].Type))
                {
                    curOriginParameterIdx++;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}