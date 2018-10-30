using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IMemberOwnerDeclaration),
        HighlightingTypes = new[]
        {
            typeof(UnityGutterMarkInfo),
            typeof(DuplicateEventFunctionWarning),
            typeof(IncorrectSignatureWarning),
            typeof(InvalidStaticModifierWarning),
            typeof(InvalidReturnTypeWarning),
            typeof(InvalidParametersWarning),
            typeof(InvalidTypeParametersWarning)
        })]
    public class UnityEventFunctionAnalyzer : MethodSignatureProblemAnalyzerBase<IMemberOwnerDeclaration>
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
                    // We'll let other inspections handle invalid signatures
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
                        if (candidate.Match == MethodSignatureMatch.ExactMatch)
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
