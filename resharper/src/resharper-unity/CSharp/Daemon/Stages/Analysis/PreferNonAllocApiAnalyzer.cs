using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[] {typeof(PreferNonAllocApiWarning)})]
    public class PreferNonAllocApiAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private static readonly IDictionary<IClrTypeName, ISet<string>> ourKnownMethods =
            new Dictionary<IClrTypeName, ISet<string>>()
            {
                {
                    KnownTypes.Physics, new HashSet<string>()
                    {
                        "CapsuleCastAll",
                        "RaycastAll",
                        "SphereCastAll",
                        "BoxCastAll",
                        "OverlapCapsule",
                        "OverlapSphere",
                        "OverlapBox",
                    }
                },
                {
                    KnownTypes.Physics2D, new HashSet<string>()
                    {
                        "LinecastAll",
                        "RaycastAll",
                        "CircleCastAll",
                        "BoxCastAll",
                        "CapsuleCastAll",
                        "GetRayIntersectionAll",
                        "OverlapPointAll",
                        "OverlapCircleAll",
                        "OverlapBoxAll",
                        "OverlapAreaAll",
                        "OverlapCapsuleAll",
                        
                    }
                }
            };
        
        
        
        public PreferNonAllocApiAnalyzer([NotNull] UnityApi unityApi) : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (!(expression.InvokedExpression is IReferenceExpression referenceExpression)) 
                return;

            var reference = expression.Reference;
            if (reference == null) 
                return;
            
            var info = reference.Resolve();
            if (info.ResolveErrorType == ResolveErrorType.OK && info.DeclaredElement is IMethod allocMethod)
            {
                var nonAllocMethod = GetNonAllocVersion(allocMethod, expression);
                if (nonAllocMethod != null)
                    consumer.AddHighlighting(new PreferNonAllocApiWarning(expression, referenceExpression, nonAllocMethod));
            }
        }

        private static IMethod GetNonAllocVersion([NotNull]IMethod method,[NotNull] IInvocationExpression expression)
        {
            var originName = method.ShortName;

            // cheap check for methods. Drop out methods with other names
            if (!ourKnownMethods[KnownTypes.Physics].Contains(originName) &&
                !ourKnownMethods[KnownTypes.Physics2D].Contains(originName))
                return null;
            
            var containingType = method.GetContainingType();
            
            if (containingType == null)
                return null;

            // drop out all other invocation
            if (!containingType.GetClrName().Equals(KnownTypes.Physics) &&
                !containingType.GetClrName().Equals(KnownTypes.Physics2D))
                return null;
            
            string newName; // xxx[All] -> xxxNonAlloc
            if (originName.EndsWith("All"))
            {
                newName = originName.Substring(0, originName.Length - 3) + "NonAlloc";
            }
            else
            {
                newName = originName + "NonAlloc";
            }

            var type = TypeFactory.CreateType(containingType);
            var table = type.GetSymbolTable(expression.PsiModule).Filter(
                new AccessRightsFilter(new DefaultAccessContext(expression)),
                new ExactNameFilter(newName),
                new PredicateFilter(t => MatchSignatureAllocToNonAlloc(method, t.GetDeclaredElement() as IMethod)));
           
            return table.GetSymbolInfos(newName).SingleOrDefault()?.GetDeclaredElement() as IMethod;
        }

        private static bool MatchSignatureAllocToNonAlloc([NotNull] IMethod method, [CanBeNull] IMethod nonAllocMethod)
        {
            // try to find method with same parameters + return value as parameter
            if (nonAllocMethod == null)
                return false;
            
            var originReturnType = method.ReturnType;
            var originParameters = method.Parameters;
            var nonAllocMethodParameters = nonAllocMethod.Parameters;
            var originSize = originParameters.Count;
            
            if (originSize + 1 != nonAllocMethodParameters.Count)
                return false;

            int curOriginParameterIdx = 0;

            foreach (var nonAllocParameter in nonAllocMethodParameters)
            {
                if (nonAllocParameter.Type.Equals(originReturnType))
                    continue;
                
                if (curOriginParameterIdx == originSize)
                    return false;
                
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