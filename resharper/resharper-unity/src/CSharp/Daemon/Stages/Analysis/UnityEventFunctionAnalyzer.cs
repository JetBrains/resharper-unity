using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
#if RIDER
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
#endif
using JetBrains.Util.dataStructures;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IMemberOwnerDeclaration),
        HighlightingTypes = new[]
        {
            typeof(DuplicateEventFunctionWarning),
            typeof(IncorrectSignatureWarning),
            typeof(InvalidStaticModifierWarning),
            typeof(InvalidReturnTypeWarning),
            typeof(InvalidParametersWarning),
            typeof(InvalidTypeParametersWarning),
            #if RIDER
            typeof(UnityCodeInsightsHighlighting)
            #else
            typeof(UnityGutterMarkInfo),
            #endif
        })]
    public class UnityEventFunctionAnalyzer : MethodSignatureProblemAnalyzerBase<IMemberOwnerDeclaration>
    {
        private readonly UnityImplicitUsageHighlightingContributor myImplicitUsageHighlightingContributor;

        public UnityEventFunctionAnalyzer(UnityApi unityApi, UnityImplicitUsageHighlightingContributor implicitUsageHighlightingContributor)
            : base(unityApi)
        {
            myImplicitUsageHighlightingContributor = implicitUsageHighlightingContributor;
        }

        protected override void Analyze(IMemberOwnerDeclaration element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var typeElement = element.DeclaredElement;
            if (typeElement == null)
                return;

            if (!Api.IsUnityType(typeElement))
                return;

            var project = element.GetProject();
            if (project == null)
                return;

            var unityVersion = Api.GetNormalisedActualVersion(project);

            var map = new CompactOneToListMap<UnityEventFunction, Candidate>(new UnityEventFunctionKeyComparer());
            foreach (var instance in typeElement.GetAllClassMembers<IMethod>())
            {
                var unityEventFunction = Api.GetUnityEventFunction(instance.Member, unityVersion, out var match);
                if (unityEventFunction != null)
                    map.AddValue(unityEventFunction, new Candidate(instance.Member, match));
            }

            foreach (var pair in map)
            {
                var function = pair.Key;
                var candidates = pair.Value;
                if (candidates.Count == 1)
                {
                    // Only one function, mark it as a unity function, even if it's not an exact match
                    // We'll let other inspections handle invalid signatures
                    var method = candidates[0].Method;
                    myImplicitUsageHighlightingContributor.AddUnityImplicitHighlightingForEventFunction(consumer, method, function);
                    AddMethodSignatureInspections(consumer, method, function, candidates[0].Match);
                }
                else
                {
                    var hasExactMatch = false;

                    // All exact matches should be marked as an event function
                    var duplicates = new FrugalLocalList<IMethod>();
                    foreach (var candidate in candidates)
                    {
                        if (candidate.Match == MethodSignatureMatch.ExactMatch)
                        {
                            hasExactMatch = true;
                            if (Equals(candidate.Method.GetContainingType(), typeElement))
                            {
                                myImplicitUsageHighlightingContributor.AddUnityImplicitHighlightingForEventFunction(
                                    consumer, candidate.Method, function);
                                duplicates.Add(candidate.Method);
                            }
                        }
                    }

                    // Multiple exact matches should be marked as duplicate/ambiguous
                    if (duplicates.Count > 1)
                    {
                        foreach (var method in duplicates)
                        {
                            foreach (var declaration in method.GetDeclarations())
                            {
                                consumer.AddHighlighting(
                                    new DuplicateEventFunctionWarning((IMethodDeclaration) declaration));
                            }
                        }
                    }

                    // If there are no exact matches, mark all as unity functions, with inspections
                    // to fix up signature errors
                    if (!hasExactMatch)
                    {
                        foreach (var candidate in candidates)
                        {
                            if (Equals(candidate.Method.GetContainingType(), typeElement))
                            {
                                var method = candidate.Method;
                                myImplicitUsageHighlightingContributor.AddUnityImplicitHighlightingForEventFunction(
                                    consumer, method, function);
                                AddMethodSignatureInspections(consumer, method, function, candidate.Match);
                            }
                        }
                    }
                }
            }
        }

        private void AddMethodSignatureInspections(IHighlightingConsumer consumer, IMethod method,
            UnityEventFunction function, MethodSignatureMatch match)
        {
            if (match == MethodSignatureMatch.NoMatch || match == MethodSignatureMatch.ExactMatch)
                return;

            var methodSignature = function.AsMethodSignature(method.Module);

            foreach (var declaration in method.GetDeclarations())
            {
                if (declaration is IMethodDeclaration methodDeclaration)
                    AddMethodSignatureInspections(consumer, methodDeclaration, methodSignature, match);
            }
        }

        private class UnityEventFunctionKeyComparer : IEqualityComparer<UnityEventFunction>
        {
            public bool Equals(UnityEventFunction x, UnityEventFunction y)
            {
                // Function name is enough. We know usage doesn't look at other types
                // ReSharper disable PossibleNullReferenceException
                return x.Name == y.Name;
                // ReSharper restore PossibleNullReferenceException
            }

            public int GetHashCode(UnityEventFunction obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        private struct Candidate
        {
            public readonly IMethod Method;
            public readonly MethodSignatureMatch Match;

            public Candidate(IMethod method, MethodSignatureMatch match)
            {
                Method = method;
                Match = match;
            }
        }
    }
}
