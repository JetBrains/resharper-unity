using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IMemberOwnerDeclaration),
        HighlightingTypes = new[] {typeof(UnityMarkOnGutter), typeof(DuplicateEventFunctionWarning)})]
    public class UnityEventFunctionAnalyzer : UnityElementProblemAnalyzer<IMemberOwnerDeclaration>
    {
        public UnityEventFunctionAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IMemberOwnerDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var typeElement = element.DeclaredElement;
            if (typeElement == null)
                return;

            if (!Api.IsUnityType(typeElement))
                return;

            var map = new OneToListMap<UnityEventFunction, IDeclaredElement>(new UnityEventFunctionKeyComparer());
            foreach (var member in typeElement.GetMembers())
            {
                var method = member as IMethod;
                if (method != null)
                {
                    var unityEventFunction = Api.GetUnityEventFunction(method);
                    if (unityEventFunction != null)
                        map.Add(unityEventFunction, method);
                }
            }

            foreach (var pair in map)
            {
                var function = pair.Key;
                var members = pair.Value;
                if (members.Count == 1)
                {
                    // Only one function, mark it as a unity function, even if it's not an exact match
                    // We'll let other inspections handle invalid signatures. Add inspections
                    var method = (IMethod) members[0];
                    AddGutterMark(consumer, method, function);
                    AddMethodSignatureInspections(consumer, method, function);
                }
                else
                {
                    var hasExactMatch = false;

                    // All exact matches should be marked as an event function
                    var duplicates = new FrugalLocalList<IMethod>();
                    foreach (var member in members)
                    {
                        var method = (IMethod) member;
                        if (function.Match(method) == EventFunctionMatch.ExactMatch)
                        {
                            AddGutterMark(consumer, method, function);
                            hasExactMatch = true;
                            duplicates.Add(method);
                        }
                    }

                    // Multiple exact matches should be marked as duplicate/ambiguous
                    if (duplicates.Count > 1)
                    {
                        foreach (var method in duplicates)
                        {
                            foreach (var declaration in method.GetDeclarations())
                            {
                                consumer.AddHighlighting(new DuplicateEventFunctionWarning((IMethodDeclaration) declaration));
                            }
                        }
                    }

                    // If there are no exact matches, mark all as unity functions, with inspections
                    // to fix up signature errors
                    if (!hasExactMatch)
                    {
                        foreach (var member in members)
                        {
                            var method = (IMethod) member;
                            AddGutterMark(consumer, method, function);
                            AddMethodSignatureInspections(consumer, method, function);
                        }
                    }
                }
            }
        }

        private void AddGutterMark(IHighlightingConsumer consumer, IMethod method, UnityEventFunction function)
        {
            foreach (var declaration in method.GetDeclarations())
                AddGutterMark(consumer, declaration, function);
        }

        private static void AddGutterMark(IHighlightingConsumer consumer, IDeclaration declaration, UnityEventFunction eventFunction)
        {
            var documentRange = declaration.GetNameDocumentRange();
            var tooltip = "Unity event function";
            if (!string.IsNullOrEmpty(eventFunction.Description))
                tooltip += Environment.NewLine + Environment.NewLine + eventFunction.Description;
            var highlighting = new UnityMarkOnGutter(declaration, documentRange, tooltip);
            consumer.AddHighlighting(highlighting, documentRange);
        }

        private static void AddMethodSignatureInspections(IHighlightingConsumer consumer, IMethod method, UnityEventFunction function)
        {
            var match = function.Match(method);

            if ((match & EventFunctionMatch.MatchingStaticModifier) != EventFunctionMatch.MatchingStaticModifier)
            {
                foreach (var declaration in method.GetDeclarations())
                {
                    var methodDeclaration = declaration as IMethodDeclaration;
                    if (methodDeclaration != null)
                        consumer.AddHighlighting(new InvalidStaticModifierWarning(methodDeclaration, function));
                }
            }

            if ((match & EventFunctionMatch.MatchingReturnType) != EventFunctionMatch.MatchingReturnType)
            {
                foreach (var declaration in method.GetDeclarations())
                {
                    var methodDeclaration = declaration as IMethodDeclaration;
                    if (methodDeclaration != null)
                        consumer.AddHighlighting(new InvalidReturnTypeWarning(methodDeclaration, function));
                }
            }

            if ((match & EventFunctionMatch.MatchingSignature) != EventFunctionMatch.MatchingSignature)
            {
                foreach (var declaration in method.GetDeclarations())
                {
                    var methodDeclaration = declaration as IMethodDeclaration;
                    if (methodDeclaration != null)
                        consumer.AddHighlighting(new InvalidSignatureWarning(methodDeclaration, function));
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
    }
}