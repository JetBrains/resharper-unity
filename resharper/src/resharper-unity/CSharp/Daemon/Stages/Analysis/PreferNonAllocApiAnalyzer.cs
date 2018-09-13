using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Resolve;

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
            
            var info = reference.Resolve();
            if (info.ResolveErrorType == ResolveErrorType.OK && info.DeclaredElement is IMethod allocMethod)
            {
                var nonAllocMethod = GetNonAllocVersion(allocMethod, expression);
                if (nonAllocMethod != null)
                {
                    consumer.AddHighlighting(new PreferNonAllocApiWarning(expression, referenceExpression, nonAllocMethod));
                }
            }
        }

        private IMethod GetNonAllocVersion(IMethod method, IInvocationExpression expression)
        {

            var originName = method.ShortName;
            if (originName.Length < 3) return null;
            
            var suffix = originName.Substring(originName.Length - 3, 3);
            var newName = (suffix.Equals("All") ? originName.Substring(0, originName.Length - 3) : originName) + "NonAlloc";

            var containingType = method.GetContainingType();
            
            if (containingType == null)
            {
                return null;
            }

            if (!containingType.GetClrName().Equals(KnownTypes.Physics) &&
                !containingType.GetClrName().Equals(KnownTypes.Physics2D))
            {
                return null;
            }

            var type = TypeFactory.CreateType(containingType);
            var table = type.GetSymbolTable(expression.PsiModule).Filter(
                new AccessRightsFilter(new DefaultAccessContext(expression)),
                new ExactNameFilter(newName),
                new PredicateFilter(t => MatchSignatureAllocToNonAlloc(method, t.GetDeclaredElement() as IMethod)));
            
           
            return table.GetSymbolInfos(newName).SingleOrDefault()?.GetDeclaredElement() as IMethod;
        }

        private bool MatchSignatureAllocToNonAlloc(IMethod method, IMethod nonAllocMethod)
        {
            // try to find method with same parameters + return value as parameter
            if (nonAllocMethod == null)
            {
                return false;
            }
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