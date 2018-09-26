using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [DaemonStage(StagesBefore = new[] {typeof(CSharpErrorStage)})]
    public class PerformanceCriticalCodeAnalysisStage : CSharpDaemonStageBase
    { 
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            var enabled = settings.GetValue((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);

            if (!enabled)
                return null;
            
            return new PerformanceCriticalCodeAnalysisProcess(process, processKind, settings, file);
        }
        
        protected override bool IsSupported(IPsiSourceFile sourceFile)
        {
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            return sourceFile.IsLanguageSupported<CSharpLanguage>();
        }
    }

    internal class PerformanceCriticalCodeAnalysisProcess : CSharpDaemonStageProcessBase
    {
        private static readonly int ourMaxAnalysisDepth = 10;
        
        private static readonly ISet<string> ourKnownHotMonoBehaviourMethods = new HashSet<string>()
        {
            "Update", "LateUpdate", "FixedUpdate",
        };

        private readonly DaemonProcessKind myProcessKind;
        [NotNull] private readonly IContextBoundSettingsStore mySettingsStore;

        public PerformanceCriticalCodeAnalysisProcess([NotNull] IDaemonProcess process, DaemonProcessKind processKind, [NotNull] IContextBoundSettingsStore settingsStore, [NotNull] ICSharpFile file)
            : base(process, file)
        {
            myProcessKind = processKind;
            mySettingsStore = settingsStore;            
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            HighlightInFile(AnalyzeFile, committer, mySettingsStore);
        }

        private void AnalyzeFile(ICSharpFile file, IHighlightingConsumer consumer)
        {
            if (myProcessKind != DaemonProcessKind.VISIBLE_DOCUMENT && myProcessKind != DaemonProcessKind.SOLUTION_ANALYSIS)
                return;

            if (!file.GetProject().IsUnityProject())
                return;
            
            var sourceFile = file.GetSourceFile();
            if (sourceFile == null)
                return;

            var hotRootMethods = new List<IMethodDeclaration>();

            // find hot methods in derived from MonoBehaviour classes.
            var descendantsEnumerator = file.Descendants();
            while (descendantsEnumerator.MoveNext())
            {
                switch (descendantsEnumerator.Current)
                {
                    // skip class declaration because all derived sym
                    case IClassLikeDeclaration classLikeDeclaration:
                        var declaredSymbol = classLikeDeclaration.DeclaredElement;
                        if (declaredSymbol == null ||
                            !declaredSymbol.GetAllSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.MonoBehaviour)))
                        {
                            descendantsEnumerator.SkipThisNode();
                        }
                        break;
                    case IMethodDeclaration methodDeclaration:
                        // check that method is hot and add it to container                        
                        var name = methodDeclaration.DeclaredElement?.ShortName;
                        if (name != null && ourKnownHotMonoBehaviourMethods.Contains(name))
                            hotRootMethods.Add(methodDeclaration);                                
                        break;
                    case IInvocationExpression invocationExpression:
                        // we should find 'StartCoroutine' method, because passed symbol will be hot too
                        var reference = (invocationExpression.InvokedExpression as IReferenceExpression)?.Reference;
                        if (reference == null)
                            break;

                        var info = reference.Resolve();
                        if (info.ResolveErrorType != ResolveErrorType.OK) 
                            break;

                        var declaredElement = info.DeclaredElement as IMethod;
                        if (declaredElement == null)
                            break;
                        
                        var containingType = declaredElement.GetContainingType();

                        if (containingType == null || containingType.GetClrName().Equals(KnownTypes.MonoBehaviour) &&
                            declaredElement.ShortName.Equals("StartCoroutine"))
                        {
                            var arguments = invocationExpression.Arguments;
                            if (arguments.Count == 0 || arguments.Count > 2)
                                break;

                            var firstArgument = arguments[0].Value;

                            // 'StartCoroutine' has overload with string. We have already attached reference, so get symbol from 
                            // reference
                            if (firstArgument.ConstantValue.IsString())
                            {
                                if (firstArgument is ILiteralExpression literalExpression)
                                {
                                    var coroutineMethodReference = literalExpression.GetReferences<UnityEventFunctionReference>().FirstOrDefault();
                                    if (coroutineMethodReference != null)
                                    {
                                        var method = coroutineMethodReference.Resolve().DeclaredElement as IMethod;
                                        if (method == null)
                                            break;
                                        
                                        var declarations = method.GetDeclarationsIn(sourceFile).Where(t => t.GetSourceFile() == sourceFile);
                                        declarations.ForEach(t => hotRootMethods.Add((IMethodDeclaration)t));
                                    }
                                }

                                // argument is IEnumerator which return from invocation, so get invocation expression declaration and add to container
                                if (firstArgument is IInvocationExpression coroutineInvocation)
                                {
                                    var invocationReference = (coroutineInvocation.InvokedExpression as IReferenceExpression)?.Reference;
                                    if (invocationReference == null)
                                        return;

                                    info = invocationReference.Resolve();
                                    if (info.ResolveErrorType != ResolveErrorType.OK)
                                        break;

                                    var method = info.DeclaredElement as IMethod;
                                    if (method == null)
                                        break;

                                    var declarations = method.GetDeclarationsIn(sourceFile).Where(t => t.GetSourceFile() == sourceFile);
                                    declarations.ForEach(t => hotRootMethods.Add((IMethodDeclaration)t));
                                }
                            }
                        }
                        
                        break;
                }
            }

            // sharing context for each hot root.
            var context = new HotMethodAnalyzerContext();

            
            foreach (var methodDeclaration in hotRootMethods)
            {
                var declaredElement = methodDeclaration.DeclaredElement.NotNull("declaredElement != null");
                context.CurrentDeclaredElement = declaredElement;
                
                var visitor = new HotMethodAnalyzer(sourceFile, consumer, ourMaxAnalysisDepth);
                methodDeclaration.ProcessDescendants(visitor, context);
            }

            // Second step of propagation 'costly reachable mark'. Handles cycles in call graph
            var callGraph = context.InvertedCallGraph;
            var nodes = new HashSet<IDeclaredElement>();
            
            callGraph.ForEach(kvp =>
            {
                if (context.IsDeclaredElementCostlyReachable(kvp.Key))
                    nodes.Add(kvp.Key);
                
                nodes.AddRange(kvp.Value.Where(t => context.IsDeclaredElementCostlyReachable(t)));
            });

            var visited = new HashSet<IDeclaredElement>();
            foreach (var node in nodes)
            {
                PropagateCostlyMark(node, callGraph, context, visited);
            }

            // highlight all invocation which indirectly calls costly methods.
            // it is fast, because we have already calculated all data
            foreach (var kvp in context.InvocationsInMethods)
            {
                var method = kvp.Key;
                if (!context.IsDeclaredElementCostlyReachable(method))
                    continue;

                var invocationElements = kvp.Value;
                foreach (var invocationElement in invocationElements)
                {
                    var invokedMethod = invocationElement.Key;
                    if (!context.IsDeclaredElementCostlyReachable(invokedMethod))
                        continue;

                    var references = invocationElement.Value;
                    foreach (var reference in references)
                    {
                        consumer.AddHighlighting(new PerformanceCriticalCodeInvocationReachableHighlighting(reference));
                    }
                }
            }
        }

        private void PropagateCostlyMark(IDeclaredElement node, IReadOnlyDictionary<IDeclaredElement, HashSet<IDeclaredElement>> callGraph,
            HotMethodAnalyzerContext context, ISet<IDeclaredElement> visited)
        {
            if (visited.Contains(node))
                return;
            visited.Add(node);
            context.MarkElementAsCostlyReachable(node);

            if (callGraph.TryGetValue(node, out var children))
            {
                foreach (var child in children)
                {
                    PropagateCostlyMark(child, callGraph, context, visited);
                }
            }
        }


        // return value only for invocation nodes. If invoked method contain costly method `true` will be returned.
        private class HotMethodAnalyzer : TreeNodeVisitor<HotMethodAnalyzerContext>, IRecursiveElementProcessor<HotMethodAnalyzerContext>
        {
            private readonly IPsiSourceFile mySourceFile;
            private readonly IHighlightingConsumer myConsumer;
            private readonly int myMaxDepth;

            public HotMethodAnalyzer(IPsiSourceFile sourceFile, IHighlightingConsumer consumer, int maxDepth)
            {
                mySourceFile = sourceFile;
                myConsumer = consumer;
                myMaxDepth = maxDepth;
            }

            public override void VisitInvocationExpression(IInvocationExpression invocationExpressionParam, HotMethodAnalyzerContext context)
            {
                // Do not add analysis for invocation expression here. Use 'AnalyzeInvocationExpression' for this goal;   
                AnalyzeInvocationExpression(invocationExpressionParam, context);
                
                // Restriction for depth of analysis
                if (myMaxDepth <= context.Depth)
                {
                    return;
                }

                var reference = (invocationExpressionParam.InvokedExpression as IReferenceExpression)?.Reference;

                var declaredElement = reference?.Resolve().DeclaredElement as IMethod;
                if (declaredElement == null)
                    return;
                
                context.RegisterInvocationInMethod(declaredElement, reference);

                // Do not visit methods twice
                if (context.IsDeclaredElementVisited(declaredElement))
                {
                    return;
                }
                    
                // find all declarations in current file
                var declarations = declaredElement.GetDeclarationsIn(mySourceFile).Where(t => t.GetSourceFile() == mySourceFile);

                // update current declared element in context and then restore it
                var originDeclaredElement = context.CurrentDeclaredElement;
                context.CurrentDeclaredElement = declaredElement;
                foreach (var declaration in declarations)
                {
                    declaration.ProcessDescendants(this, context);
                }

                context.CurrentDeclaredElement = originDeclaredElement;

                // propagate costly reachable methods back
                // Note : on this step there is methods that can be marked as costly reachable later (e.g. recursion)
                // so we will propagate costly reachable mark later
                if (context.IsDeclaredElementCostlyReachable(declaredElement))
                {
                    context.MarkCurrentAsCostlyReachable();
                }
                context.MarkCurrentAsVisited();
            }

            private void AnalyzeInvocationExpression(IInvocationExpression invocationExpressionParam, HotMethodAnalyzerContext context)
            {
                var reference = (invocationExpressionParam.InvokedExpression as IReferenceExpression)?.Reference;

                var declaredElement = reference?.Resolve().DeclaredElement as IMethod;
                if (declaredElement == null)
                    return;

                var containingType = declaredElement.GetContainingType();
                if (containingType == null)
                    return;

                #region CostlyMethodInvocationInspection

                ISet<string> knownCostlyMethods = null;
                if (containingType.GetClrName().Equals(KnownTypes.Component))
                    knownCostlyMethods = ourKnownComponentCostlyMethods;

                if (containingType.GetClrName().Equals(KnownTypes.GameObject))
                    knownCostlyMethods = ourKnownGameObjectCostlyMethods;
                
                if (containingType.GetClrName().Equals(KnownTypes.Resources))
                    knownCostlyMethods = ourKnownResourcesCostlyMethods;
                
                if (containingType.GetClrName().Equals(KnownTypes.Object))
                    knownCostlyMethods = ourKnownObjectCostlyMethods;
                
                if (containingType.GetClrName().Equals(KnownTypes.Transform))
                    knownCostlyMethods = ourKnownTransformCostlyMethods;
                
                if (knownCostlyMethods == null)
                    return;
                
                var shortName = declaredElement.ShortName;

                if (knownCostlyMethods.Contains(shortName))
                {
                    context.MarkCurrentAsCostlyReachable();
                    myConsumer.AddHighlighting(new PerformanceCriticalCodeInvocationHighlighting(reference));
                }
                #endregion
                
                #region AddMonoBehaviourInspection
                
                if (containingType.GetClrName().Equals(KnownTypes.GameObject))
                {
                    if (shortName.Equals("AddComponent") && invocationExpressionParam.TypeArguments.Count == 1)
                    {
                        context.MarkCurrentAsCostlyReachable();
                        myConsumer.AddHighlighting(new PerformanceCriticalCodeInvocationHighlighting(reference));
                    }
                }
                
                #endregion
            }


            public override void VisitEqualityExpression(IEqualityExpression equalityExpressionParam, HotMethodAnalyzerContext context)
            {
                #region NullComprasionUnityObjectInspection
                var reference = equalityExpressionParam.Reference;
                if (reference == null)
                    return;
                
                var isNullFound = false;
                var leftOperand = equalityExpressionParam.LeftOperand;
                var rightOperand = equalityExpressionParam.RightOperand;
                
                if (leftOperand == null || rightOperand == null)
                    return;
                
                IExpressionType expressionType = null;
                
                if (leftOperand.ConstantValue(new UniversalContext(equalityExpressionParam)).IsNull())
                {
                    isNullFound = true;
                    expressionType = rightOperand.GetExpressionType();
                  
                }
                else if (rightOperand.ConstantValue(new UniversalContext(equalityExpressionParam)).IsNull())
                {
                    isNullFound = true;
                    expressionType = leftOperand.GetExpressionType();
                }

                if (!isNullFound)
                    return;
                
                var typeElement = expressionType.ToIType()?.GetTypeElement();
                if (typeElement == null)
                    return;

                if (typeElement.GetAllSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.Object)))
                    myConsumer.AddHighlighting(new PerformanceCriticalCodeInvocationHighlighting(reference));
                #endregion
            }

            public bool InteriorShouldBeProcessed(ITreeNode element, HotMethodAnalyzerContext context)
            {
                return !(element is ICSharpClosure);
            }

            public bool IsProcessingFinished(HotMethodAnalyzerContext context)
            {
                return false;
            }

            public void ProcessBeforeInterior(ITreeNode element, HotMethodAnalyzerContext context)
            {
                // handle depth of analysis
                if (element is IInvocationExpression)
                    context.Depth++;
                
                if (element is ICSharpTreeNode node)
                    node.Accept(this, context);
            }

            public void ProcessAfterInterior(ITreeNode element, HotMethodAnalyzerContext context)
            {
                // handle depth of analysis
                if (element is IInvocationExpression)
                    context.Depth--;
            }
            
            #region data
            
            private static readonly ISet<string> ourKnownComponentCostlyMethods = new HashSet<string>()
            {
                "GetComponentInChildren",
                "GetComponentInParent",
                "GetComponentsInChildren",
            };
            
            private static readonly ISet<string> ourKnownGameObjectCostlyMethods = new HashSet<string>()
            {
                "Find",
                "FindGameObjectsWithTag",
                "FindGameObjectWithTag",
                "FindWithTag",
                "GetComponent",
                "GetComponents",
                "GetComponentsInParent"
            };
            
            private static readonly ISet<string> ourKnownTransformCostlyMethods = new HashSet<string>()
            {
                "Find"
            };
            
            private static readonly ISet<string> ourKnownResourcesCostlyMethods = new HashSet<string>()
            {
                "FindObjectsOfTypeAll",
            };
            
            private static readonly ISet<string> ourKnownObjectCostlyMethods = new HashSet<string>()
            {
                "FindObjectsOfType",
                "FindObjectOfType",
                "FindObjectsOfTypeIncludingAssets",
            };
            
            #endregion
        }

        private class HotMethodAnalyzerContext
        {
            // Current depth of analysis
            public int Depth { get; set; }
            
            // Inverted call graph for costly reachable mark propagation after visit AST
            public IReadOnlyDictionary<IDeclaredElement, HashSet<IDeclaredElement>> InvertedCallGraph 
                => new ReadOnlyDictionary<IDeclaredElement, HashSet<IDeclaredElement>>(myInvertedCallGraph);
            
            
            // Container of all invoked method for specified method with node elements. Helper for highlighting after 
            // propagation of costly reachable mark is done
            public readonly Dictionary<IDeclaredElement, Dictionary<IDeclaredElement, List<IReference>>> InvocationsInMethods 
                = new Dictionary<IDeclaredElement, Dictionary<IDeclaredElement, List<IReference>>>(); 
            
            // Visited nodes
            private readonly ISet<IDeclaredElement> myVisited = new HashSet<IDeclaredElement>();
            
            // Helper for answer question : Is declared element has costly reachable mark?
            private readonly HashSet<IDeclaredElement> myCostlyMethods = new HashSet<IDeclaredElement>();
            private readonly Dictionary<IDeclaredElement, HashSet<IDeclaredElement>> myInvertedCallGraph = new Dictionary<IDeclaredElement, HashSet<IDeclaredElement>>();

            
            public IDeclaredElement CurrentDeclaredElement { get; set; }
            
            public bool IsDeclaredElementVisited(IDeclaredElement element)
            {
                return myVisited.Contains(element);
            }

            public void MarkCurrentAsVisited()
            {
                myVisited.Add(CurrentDeclaredElement);
            }

            public void MarkCurrentAsCostlyReachable()
            {
                myCostlyMethods.Add(CurrentDeclaredElement);
            }
            
            public void MarkElementAsCostlyReachable(IDeclaredElement element)
            {
                myCostlyMethods.Add(element);
            }

            public bool IsCurrentDeclaredElementCostlyReachable()
            {
                return myCostlyMethods.Contains(CurrentDeclaredElement);
            }
            
            public bool IsDeclaredElementCostlyReachable(IDeclaredElement element)
            {
                return myCostlyMethods.Contains(element);
            }

            public void RegisterInvocationInMethod(IDeclaredElement invokedMethod, IReference reference)
            {
                var method = CurrentDeclaredElement;
                // remember which methods was invoked from `method`. We will highlight them at the end of analysis if they are marked as `costly reachable`
                if (!InvocationsInMethods.ContainsKey(method))
                    InvocationsInMethods[method] = new Dictionary<IDeclaredElement, List<IReference>>();

                var invocationsGroupedByDeclaredElement = InvocationsInMethods[method];
                if (!invocationsGroupedByDeclaredElement.ContainsKey(invokedMethod)) 
                    invocationsGroupedByDeclaredElement[invokedMethod] = new List<IReference>();

                var group = invocationsGroupedByDeclaredElement[invokedMethod];
                group.Add(reference);

                // add edge to inverted call graph
                if (!myInvertedCallGraph.ContainsKey(invokedMethod))
                    myInvertedCallGraph[invokedMethod] = new HashSet<IDeclaredElement>();
                myInvertedCallGraph[invokedMethod].Add(method);
            }
        } 
    }
}