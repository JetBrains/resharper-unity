using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IMethodDeclaration),
        HighlightingTypes = new[] {typeof(UnityEventFunctionInheritanceMarkOnGutter)})]
    public class HiddenEventFunctionProblemAnalyzer : UnityElementProblemAnalyzer<IMethodDeclaration>
    {
        public HiddenEventFunctionProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IMethodDeclaration element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            var method = element.DeclaredElement;
            if (method == null)
                return;

            var eventFunction = Api.GetUnityEventFunction(method);
            if (eventFunction == null)
                return;

            var typeElement = method.GetContainingType();
            if (typeElement == null)
                return;

            UnityEventFunctionInheritanceMarkOnGutter mark = null;
            foreach (var instance in typeElement.GetAllClassMembers(method.ShortName))
            {
                if (Equals(instance.Member, method))
                    continue;

                if (!(instance.Member is IMethod baseMethod))
                    continue;

                // If the implementation is anything other than private, it's accessible, and R#'s standard inspections
                // will highlight it. This isn't true for internal, though...
                if (baseMethod.GetAccessRights() != AccessRights.PRIVATE)
                    continue;

                if (eventFunction.Match(baseMethod) == MethodSignatureMatch.ExactMatch)
                {
                    (mark ?? (mark = new UnityEventFunctionInheritanceMarkOnGutter(element))).AddHiddenMember(
                        instance.Member);
                }
            }

            if (mark != null)
                consumer.AddHighlighting(mark);
        }

        [StaticSeverityHighlighting(Severity.INFO, typeof(HighlightingGroupIds.GutterMarks),
            OverlapResolve = OverlapResolveKind.NONE, ShowToolTipInStatusBar = false)]
        private class UnityEventFunctionInheritanceMarkOnGutter : InheritanceMarkOnGutter, IUnityHighlighting
        {
            public UnityEventFunctionInheritanceMarkOnGutter(IDeclaration inheritor)
                : base(inheritor)
            {
            }

            protected override string KindName(ITypeMember declaredElement) => "Unity event function";
        }
    }
}