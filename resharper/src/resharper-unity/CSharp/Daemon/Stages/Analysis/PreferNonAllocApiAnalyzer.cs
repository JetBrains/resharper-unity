using System.Collections.Generic;
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
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[] {typeof(PreferNonAllocApiWarning)})]
    public class PreferNonAllocApiAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private static readonly IDictionary<string, string> ourPhysicsKnownMethods =
            new Dictionary<string, string>()
            {
                {"CapsuleCastAll", "CapsuleCastNonAlloc"},
                {"RaycastAll", "RaycastNonAlloc"},
                {"SphereCastAll", "SphereCastNonAlloc"},
                {"BoxCastAll", "BoxCastNonAlloc"},
                {"OverlapCapsule", "OverlapCapsuleNonAlloc"},
                {"OverlapSphere", "OverlapSphereNonAlloc"},
                {"OverlapBox", "OverlapBoxNonAlloc"},
            };
        
        private static readonly IDictionary<string, string> ourPhysics2DKnownMethods =
            new Dictionary<string, string>()
            {
                {"LinecastAll", "CapsuleCastNonAlloc"},
                {"RaycastAll", "RaycastNonAlloc"},
                {"CircleCastAll", "CircleCastNonAlloc"},
                {"BoxCastAll", "BoxCastNonAlloc"},
                {"CapsuleCastAll", "CapsuleCastNonAlloc"},
                {"GetRayIntersectionAll", "GetRayIntersectionNonAlloc"},
                {"OverlapPointAll", "OverlapPointNonAlloc"},
                {"OverlapCircleAll", "OverlapCircleNonAlloc"},
                {"OverlapBoxAll", "OverlapBoxNonAlloc"},
                {"OverlapAreaAll", "OverlapAreaNonAlloc"},
                {"OverlapCapsuleAll", "OverlapCapsuleNonAlloc"},
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
            if (!ourPhysicsKnownMethods.ContainsKey(originName) &&
                !ourPhysics2DKnownMethods.ContainsKey(originName))
                return null;
            
            var containingType = method.GetContainingType();
            
            if (containingType == null)
                return null;

            // drop out all other invocation and get name
            string newName; // xxx[All] -> xxxNonAlloc
            var containingTypeName = containingType.GetClrName();
            if (containingTypeName.Equals(KnownTypes.Physics))
            {
                ourPhysicsKnownMethods.TryGetValue(originName, out newName);
            } else if (containingTypeName.Equals(KnownTypes.Physics2D))
            {
                ourPhysics2DKnownMethods.TryGetValue(originName, out newName);
            }
            else
            {
                return null;
            }

            if (newName == null)
                return null;
            
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