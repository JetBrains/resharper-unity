using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IMemberOwnerDeclaration),
        HighlightingTypes = new[]
        {
            typeof(UnityGutterMarkInfo),
            typeof(DuplicateEventFunctionWarning),
            typeof(InvalidStaticModifierWarning),
            typeof(InvalidReturnTypeWarning),
            typeof(InvalidParametersWarning),
            typeof(InvalidTypeParametersWarning)
        })]
    public class UnityEventFunctionAnalyzer : UnityElementProblemAnalyzer<IMemberOwnerDeclaration>
    {
        public UnityEventFunctionAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IMemberOwnerDeclaration element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var typeElement = element.DeclaredElement;
            if (typeElement == null)
                return;

            if (!Api.IsUnityType(typeElement))
                return;

            var map = new OneToListMap<UnityEventFunction, Candidate>(new UnityEventFunctionKeyComparer());
            foreach (var member in typeElement.GetMembers())
            {
                if (member is IMethod method)
                {
                    var unityEventFunction = Api.GetUnityEventFunction(method, out var match);
                    if (unityEventFunction != null)
                        map.Add(unityEventFunction, new Candidate(method, match));
                }
            }

            foreach (var pair in map)
            {
                var function = pair.Key;
                var candidates = pair.Value;
                if (candidates.Count == 1)
                {
                    // Only one function, mark it as a unity function, even if it's not an exact match
                    // We'll let other inspections handle invalid signatures. Add inspections
                    var method = candidates[0].Method;
                    AddGutterMark(consumer, method, function);
                    AddMethodSignatureInspections(consumer, method, function, candidates[0].Match);
                }
                else
                {
                    var hasExactMatch = false;

                    // All exact matches should be marked as an event function
                    var duplicates = new FrugalLocalList<IMethod>();
                    foreach (var candidate in candidates)
                    {
                        if (candidate.Match == EventFunctionMatch.ExactMatch)
                        {
                            AddGutterMark(consumer, candidate.Method, function);
                            hasExactMatch = true;
                            duplicates.Add(candidate.Method);
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
                            var method = candidate.Method;
                            AddGutterMark(consumer, method, function);
                            AddMethodSignatureInspections(consumer, method, function, candidate.Match);
                        }
                    }
                }
            }
        }

        private void AddGutterMark(IHighlightingConsumer consumer, IMethod method, UnityEventFunction function)
        {
            foreach (var declaration in method.GetDeclarations())
                AddGutterMark(declaration, function, consumer);
        }

        private void AddGutterMark(IDeclaration declaration, UnityEventFunction eventFunction,
            IHighlightingConsumer consumer)
        {
            var tooltip = "Unity event function";
            if (!string.IsNullOrEmpty(eventFunction.Description))
                tooltip += Environment.NewLine + Environment.NewLine + eventFunction.Description;
            if (eventFunction.Coroutine)
                tooltip += Environment.NewLine + "This function can be a coroutine.";

            var highlighting = new UnityGutterMarkInfo(declaration, tooltip);
            consumer.AddHighlighting(highlighting);
        }

        private static void AddMethodSignatureInspections(IHighlightingConsumer consumer, IMethod method,
            UnityEventFunction function, EventFunctionMatch match)
        {
            if (match == EventFunctionMatch.NoMatch || match == EventFunctionMatch.ExactMatch)
                return;

            var methodSignature = function.AsMethodSignature(method.Module);

            foreach (var declaration in method.GetDeclarations())
            {
                if (declaration is IMethodDeclaration methodDeclaration)
                {
                    if ((match & EventFunctionMatch.MatchingStaticModifier) != EventFunctionMatch.MatchingStaticModifier)
                        consumer.AddHighlighting(new InvalidStaticModifierWarning(methodDeclaration, methodSignature));
                    if ((match & EventFunctionMatch.MatchingReturnType) != EventFunctionMatch.MatchingReturnType)
                        consumer.AddHighlighting(new InvalidReturnTypeWarning(methodDeclaration, methodSignature));
                    if ((match & EventFunctionMatch.MatchingSignature) != EventFunctionMatch.MatchingSignature)
                        consumer.AddHighlighting(new InvalidParametersWarning(methodDeclaration, methodSignature));
                    if ((match & EventFunctionMatch.MatchingTypeParameters) != EventFunctionMatch.MatchingTypeParameters)
                        consumer.AddHighlighting(new InvalidTypeParametersWarning(methodDeclaration, methodSignature));
                }
            }
        }

        private class UnityEventFunctionKeyComparer : IEqualityComparer<UnityEventFunction>
        {
            public bool Equals(UnityEventFunction x, UnityEventFunction y)
            {
                // Function name is enough. We know usage doesn't look at other types
                return x.Name == y.Name;
            }

            public int GetHashCode(UnityEventFunction obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        private struct Candidate
        {
            public readonly IMethod Method;
            public readonly EventFunctionMatch Match;

            public Candidate(IMethod method, EventFunctionMatch match)
            {
                Method = method;
                Match = match;
            }
        }
    }
}
