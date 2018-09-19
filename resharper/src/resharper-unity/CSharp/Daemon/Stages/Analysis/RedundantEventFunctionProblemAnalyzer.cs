using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    // Empty event functions are still called, which is a performance overhead
    // https://blogs.unity3d.com/2015/12/23/1k-update-calls/
    [ElementProblemAnalyzer(typeof(IMethodDeclaration), HighlightingTypes = new[] { typeof(RedundantEventFunctionWarning) })]
    public class RedundantEventFunctionProblemAnalyzer : UnityElementProblemAnalyzer<IMethodDeclaration>
    {
        public RedundantEventFunctionProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IMethodDeclaration methodDeclaration, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var method = methodDeclaration.DeclaredElement;
            if (method == null) return;
            if (IsEventFunction(method) && HasEmptyBody(methodDeclaration) && !IsHidingBaseMethods(method) &&
                !method.IsOverride && !IsInheritedVirtualMethod(methodDeclaration, method, data))
            {
                consumer.AddHighlighting(new RedundantEventFunctionWarning(methodDeclaration));
            }
        }

        private bool IsEventFunction(IMethod method)
        {
            Api.GetUnityEventFunction(method, out var match);
            return match == MethodSignatureMatch.ExactMatch;
        }

        private bool HasEmptyBody(IMethodDeclaration method)
        {
            if (method.ContainsPreprocessorDirectives())
                return false;

            var codeBody = method.GetCodeBody();

            // Don't mark it as redundant if it's not finished
            if (codeBody.IsEmpty || (codeBody.BlockBody != null && !IsFinished(codeBody.BlockBody)))
                return false;

            if (codeBody.BlockBody != null && codeBody.BlockBody.Statements.Any())
                return false;

            // We just plain don't support C#6 expression bodied members
            if (codeBody.ExpressionBody != null)
                return false;

            return true;
        }

        private bool IsFinished(IBlock block)
        {
            // We can't use codeBody.IsFinished, as this will also check for statements!
            return block.LBrace != null && block.RBrace != null;
        }

        private bool IsHidingBaseMethods(IMethod method)
        {
            return method.GetHiddenMembers().Count != 0;
        }

        private bool IsInheritedVirtualMethod(IMethodDeclaration methodDeclaration, IMethod method,
            ElementProblemAnalyzerData data)
        {
            if (method.CanBeOverridden() == false)
                return false;

            var consumer = new FindFirstResultConsumer();
            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(data.Solution, false);
            var finder = methodDeclaration.GetPsiServices().Finder;
            finder.FindImmediateImplementingMembers(method, searchDomain, consumer, true,
                new ProgressIndicatorWithInterruptChecker(() =>
                {
                    try
                    {
                        data.ThrowIfInterrupted();
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                }));
            return consumer.FoundImplementingMethod;
        }

        private class FindFirstResultConsumer : IFindResultConsumer<IDeclaredElement>
        {
            public bool FoundImplementingMethod { get; private set; }

            public IDeclaredElement Build(FindResult result)
            {
                if (result is FindResultOverridableMember fr)
                    return fr.DeclaredElement;
                return null;
            }

            public FindExecution Merge(IDeclaredElement data)
            {
                FoundImplementingMethod = true;
                return FindExecution.Stop;
            }
        }
    }
}