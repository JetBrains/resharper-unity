using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [DaemonStage(StagesBefore = new[] {typeof(CSharpErrorStage), typeof(GlobalFileStructureCollectorStage)})]
    public class PerformanceCriticalCodeAnalysisStage : CSharpDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            var enabled = processKind == DaemonProcessKind.VISIBLE_DOCUMENT && settings.GetValue((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);

            if (!enabled)
                return null;

            return new PerformanceCriticalCodeAnalysisProcess(process, file);
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

        private readonly Func<bool> myCheckForInterrupt;

        public PerformanceCriticalCodeAnalysisProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file)
            : base(process, file)
        {
            myCheckForInterrupt = InterruptableActivityCookie.GetCheck().NotNull();
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            var highlightingConsumer = new FilteringHighlightingConsumer(new PerformanceHighlightingConsumer(DaemonProcess.SourceFile, File),DaemonProcess.SourceFile, File, DaemonProcess.ContextBoundSettingsStore);
            AnalyzeFile(File, highlightingConsumer);
            committer(new DaemonStageResult(highlightingConsumer.Highlightings));
        }

        private void AnalyzeFile(ICSharpFile file, IHighlightingConsumer consumer)
        {
            if (!file.GetProject().IsUnityProject())
                return;

            var sourceFile = file.GetSourceFile();
            if (sourceFile == null)
                return;

            // find hot methods in derived from MonoBehaviour classes.
            var hotRootMethods = FindHotRootMethods(file, sourceFile);
            if (hotRootMethods.Count == 0) return;
            myCheckForInterrupt();

            var context = GetHotMethodAnalyzerContext(consumer, hotRootMethods, sourceFile);
            myCheckForInterrupt();

            // Second step of propagation 'costly reachable mark'. Handles cycles in call graph
            PropagateCostlyReachableMark(context);
            myCheckForInterrupt();

            // highlight hot methods
            foreach (var hotMethodDeclaration in context.HotMethods)
            {
                HighlightHotMethod(hotMethodDeclaration, consumer);
            }

            // highlight all invocation which indirectly calls costly methods.
            // it is fast, because we have already calculated all data
            HighlightCostlyReachableInvocation(consumer, context);
        }

        private static void HighlightCostlyReachableInvocation(IHighlightingConsumer consumer, HotMethodAnalyzerContext context)
        {
            foreach (var kvp in context.InvocationsInMethods)
            {
                var method = kvp.Key;
                if (!context.IsDeclaredElementCostly(method))
                    continue;

                var invocationElements = kvp.Value;
                foreach (var invocationElement in invocationElements)
                {
                    var invokedMethod = invocationElement.Key;
                    if (!context.IsDeclaredElementCostly(invokedMethod))
                        continue;

                    var references = invocationElement.Value;
                    foreach (var reference in references)
                    {
                        var referenceExpression = reference.InvokedExpression as IReferenceExpression;
                        if (referenceExpression == null)
                            continue;
                        consumer.AddHighlighting(new PerformanceInvocationHighlighting(reference,
                            referenceExpression.Reference.NotNull("(reference.InvokedExpression as IReferenceExpression).Reference != null")));
                    }
                }
            }
        }

        private void PropagateCostlyReachableMark(HotMethodAnalyzerContext context)
        {
            var callGraph = context.InvertedCallGraph;
            var nodes = new HashSet<IDeclaredElement>();

            foreach (var costlyMethod in context.CostlyMethods)
            {
                nodes.Add(costlyMethod);
            }

            var visited = new HashSet<IDeclaredElement>();
            foreach (var node in nodes)
            {
                PropagateCostlyMark(node, callGraph, context, visited);
            }
        }

        private void PropagateCostlyMark(IDeclaredElement node, IReadOnlyDictionary<IDeclaredElement, HashSet<IDeclaredElement>> callGraph,
            HotMethodAnalyzerContext context, ISet<IDeclaredElement> visited)
        {
            if (visited.Contains(node))
                return;

            visited.Add(node);
            context.MarkElementAsCostly(node);

            if (callGraph.TryGetValue(node, out var children))
            {
                foreach (var child in children)
                {
                    PropagateCostlyMark(child, callGraph, context, visited);
                }
            }
        }

        private void HighlightHotMethod(IDeclaration node, IHighlightingConsumer consumer)
        {
            // ReSharper doesn't support LINE_MARKER highlightings, so we use an underline effect instead. Remove this
            // once ReSharper supports LINE_MARKER. See PerformanceCriticalCodeHighlighters.cs
#if RIDER
            var range = node.GetDocumentRange();
#else
            var range = node.GetNameDocumentRange();
#endif
            consumer.AddHighlighting(new PerformanceHighlighting(range));
        }

        private HotMethodAnalyzerContext GetHotMethodAnalyzerContext(IHighlightingConsumer consumer, HashSet<IDeclaration> hotRootMethods,
            IPsiSourceFile sourceFile)
        {
            // sharing context for each hot root.
            var context = new HotMethodAnalyzerContext();

            foreach (var declaration in hotRootMethods)
            {
                var declaredElement = declaration.DeclaredElement;
                if (declaredElement == null)
                    continue;

                context.CurrentDeclaredElement = declaredElement;
                context.CurrentDeclaration = declaration;

                var visitor = new HotMethodAnalyzer(sourceFile, consumer, myCheckForInterrupt, ourMaxAnalysisDepth);
                declaration.ProcessThisAndDescendants(visitor, context);
            }

            return context;
        }

        private HashSet<IDeclaration> FindHotRootMethods([NotNull]ICSharpFile file,[NotNull] IPsiSourceFile sourceFile)
        {
            var api = file.GetSolution().GetComponent<UnityApi>();
            var result = new HashSet<IDeclaration>();

            var descendantsEnumerator = file.Descendants();
            while (descendantsEnumerator.MoveNext())
            {
                switch (descendantsEnumerator.Current)
                {
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
                        var declaredElement = methodDeclaration.DeclaredElement;
                        if (declaredElement == null)
                            break;
                        var name = declaredElement.ShortName;
                        if (ourKnownHotMonoBehaviourMethods.Contains(name) && api.IsEventFunction(declaredElement))
                            result.Add(methodDeclaration);
                        break;

                    case IInvocationExpression invocationExpression:
                        // we should find 'StartCoroutine' method, because passed symbol will be hot too
                        var reference = (invocationExpression.InvokedExpression as IReferenceExpression)?.Reference;
                        if (reference == null)
                            break;

                        var info = reference.Resolve();
                        if (info.ResolveErrorType != ResolveErrorType.OK)
                            break;

                        declaredElement = info.DeclaredElement as IMethod;
                        if (declaredElement == null)
                            break;

                        var containingType = declaredElement.GetContainingType();

                        if (containingType == null || containingType.GetClrName().Equals(KnownTypes.MonoBehaviour))
                        {
                            if (!declaredElement.ShortName.Equals("StartCoroutine") &&
                                !declaredElement.ShortName.Equals("InvokeRepeating"))
                            {
                                break;
                            }

                            var firstArgument = invocationExpression.Arguments.FirstOrDefault()?.Value;
                            if (firstArgument == null)
                                break;
                            
                            var coroutineMethodDeclaration = ExtractMethodDeclarationFromStartCoroutine(firstArgument);
                            if (coroutineMethodDeclaration == null)
                                break;

                            var declarations = coroutineMethodDeclaration.GetDeclarationsIn(sourceFile);
                            foreach (var declaration in declarations)
                            {
                                result.Add(declaration);
                            }
                        } 
                        break;
                }
            }

            return result;
        }

        private IDeclaredElement ExtractMethodDeclarationFromStartCoroutine([NotNull]ICSharpExpression firstArgument)
        {
            // 'StartCoroutine' has overload with string. We have already attached reference, so get declaration from
            // reference
            if (firstArgument is ILiteralExpression literalExpression)
            {
                var coroutineMethodReference = literalExpression.GetReferences<UnityEventFunctionReference>().FirstOrDefault();
                if (coroutineMethodReference != null)
                {
                    return coroutineMethodReference.Resolve().DeclaredElement;
                }
            }

            // argument is IEnumerator which is returned from invocation, so get invocation declaration
            if (firstArgument is IInvocationExpression coroutineInvocation)
            {
                var invocationReference = (coroutineInvocation.InvokedExpression as IReferenceExpression)?.Reference;
                var info = invocationReference?.Resolve();
                return info?.DeclaredElement;
            }

            return null;
        }

        private class HotMethodAnalyzer : TreeNodeVisitor<HotMethodAnalyzerContext>, IRecursiveElementProcessor<HotMethodAnalyzerContext>
        {
            private readonly IPsiSourceFile mySourceFile;
            private readonly IHighlightingConsumer myConsumer;
            private readonly Func<bool> myCheckForInterrupt;
            private readonly int myMaxDepth;

            public HotMethodAnalyzer(IPsiSourceFile sourceFile, IHighlightingConsumer consumer, Func<bool> checkForInterrupt, int maxDepth)
            {
                mySourceFile = sourceFile;
                myConsumer = consumer;
                myCheckForInterrupt = checkForInterrupt;
                myMaxDepth = maxDepth;
            }

            public override void VisitInvocationExpression(IInvocationExpression invocationExpressionParam, HotMethodAnalyzerContext context)
            {
                // Do not add analysis for invocation expression here. Use 'AnalyzeInvocationExpression' for this goal;
                AnalyzeInvocationExpression(invocationExpressionParam, context);

                // Restriction for depth of analysis
                if (myMaxDepth <= context.Depth)
                    return;

                var reference = (invocationExpressionParam.InvokedExpression as IReferenceExpression)?.Reference;

                var declaredElement = reference?.Resolve().DeclaredElement;
                if (declaredElement == null)
                    return;

                context.RegisterInvocationInMethod(declaredElement, invocationExpressionParam);
                RegisterImplicitInvocation(invocationExpressionParam, context);
                
                HandleInvokedMethod(declaredElement, context);
            }

            private void HandleInvokedMethod(IDeclaredElement declaredElement, HotMethodAnalyzerContext context)
            {
                // find all declarations in current file
                var declarations = declaredElement.GetDeclarationsIn(mySourceFile);

                // update current declared element in context and then restore it
                var originDeclaredElement = context.CurrentDeclaredElement;
                var originDeclaration = context.CurrentDeclaration;

                foreach (var declaration in declarations)
                {
                    context.CurrentDeclaredElement = declaredElement;
                    context.CurrentDeclaration = declaration;

                    // Do not visit methods twice
                    if (!context.IsCurrentElementVisited())
                    {
                        myCheckForInterrupt();
                        declaration.ProcessThisAndDescendants(this, context);
                    }
                }


                context.CurrentDeclaration = originDeclaration;
                context.CurrentDeclaredElement = originDeclaredElement;

                // propagate costly reachable methods back
                // Note : on this step there is methods that can be marked as costly reachable later (e.g. recursion)
                // so we will propagate costly reachable mark later
                if (context.IsDeclaredElementCostly(declaredElement))
                {
                    context.MarkCurrentAsCostly();
                }
            }

            private void RegisterImplicitInvocation(IInvocationExpression invocationExpression, HotMethodAnalyzerContext context)
            {
                var name = invocationExpression.Reference?.Resolve().DeclaredElement?.ShortName;
                if (name == null)
                    return;

                if (name.Equals("Invoke") || name.Equals("InvokeRepeating"))
                {
                    var implicitlyInvokeDeclaredElement = invocationExpression.Arguments.FirstOrDefault()?.Value
                        ?.GetReferences<UnityEventFunctionReference>().FirstOrDefault()?.Resolve().DeclaredElement;
                    if (implicitlyInvokeDeclaredElement == null)
                        return;

                    HandleInvokedMethod(implicitlyInvokeDeclaredElement, context);
                }
            }

            private void AnalyzeInvocationExpression(IInvocationExpression invocationExpressionParam, HotMethodAnalyzerContext context)
            {
                var reference = (invocationExpressionParam.InvokedExpression as IReferenceExpression)?.Reference;
                if (reference == null)
                    return;

                var declaredElement = reference.Resolve().DeclaredElement as IMethod;

                var containingType = declaredElement?.GetContainingType();
                if (containingType == null)
                    return;

                if (PerformanceCriticalCodeStageUtil.IsInvocationExpensive(invocationExpressionParam))
                {
                    context.MarkCurrentAsCostly();
                    myConsumer.AddHighlighting(new PerformanceInvocationHighlighting(invocationExpressionParam, reference));
                }
            }

            public override void VisitReferenceExpression(IReferenceExpression expression, HotMethodAnalyzerContext context)
            {
                if (expression.NameIdentifier?.Name == "main")
                {
                    var info = expression.Reference.Resolve();
                    if (info.ResolveErrorType == ResolveErrorType.OK)
                    {
                        var property = info.DeclaredElement as IProperty;
                        var containingType = property?.GetContainingType();
                        if (containingType != null && KnownTypes.Camera.Equals(containingType.GetClrName()))
                        {
                            context.MarkCurrentAsCostly();
                            myConsumer.AddHighlighting(new PerformanceCameraMainHighlighting(expression));
                        }
                    }
                }
            }

            public override void VisitEqualityExpression(IEqualityExpression equalityExpressionParam, HotMethodAnalyzerContext context)
            {
                CheckNullComparisonWithUnityObject(equalityExpressionParam, context);
            }

            public bool InteriorShouldBeProcessed(ITreeNode element, HotMethodAnalyzerContext context)
            {
                if (element is ICSharpClosure closure)
                    return Equals(closure.DeclaredElement, context.CurrentDeclaredElement);

                return true;
            }

            public bool IsProcessingFinished(HotMethodAnalyzerContext context)
            {
                return false;
            }

            public void ProcessBeforeInterior(ITreeNode element, HotMethodAnalyzerContext context)
            {
                if (element is IDeclaration declaration && declaration == context.CurrentDeclaration)
                   context.MarkCurrentAsVisited();

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

            private void CheckNullComparisonWithUnityObject([NotNull]IEqualityExpression equalityExpressionParam, HotMethodAnalyzerContext context)
            {
                var reference = equalityExpressionParam.Reference;
                if (reference == null)
                    return;

                var isNullFound = false;
                var leftOperand = equalityExpressionParam.LeftOperand;
                var rightOperand = equalityExpressionParam.RightOperand;

                if (leftOperand == null || rightOperand == null)
                    return;

                ICSharpExpression expression = null;

                if (leftOperand.ConstantValue.IsNull())
                {
                    isNullFound = true;
                    expression = rightOperand;

                }
                else if (rightOperand.ConstantValue.IsNull())
                {
                    isNullFound = true;
                    expression = leftOperand;
                }

                if (!isNullFound)
                    return;

                var typeElement = expression.GetExpressionType().ToIType()?.GetTypeElement();
                if (typeElement == null)
                    return;

                if (typeElement.GetAllSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.Object)))
                {
                    context.MarkCurrentAsCostly();

                    var suffix = equalityExpressionParam.EqualityType == EqualityExpressionType.NE ? "NotNull" : "Null";

                    string baseName = null;
                    if (expression is IReferenceExpression referenceExpression)
                    {
                        baseName = referenceExpression.NameIdentifier.Name;
                    }
                    else
                    {
                        baseName = typeElement.ShortName;
                    }

                    var variableName = "is" + baseName + suffix;
                    myConsumer.AddHighlighting(new PerformanceNullComparisonHighlighting(equalityExpressionParam, variableName, reference));
                }
            }
        }

        private class HotMethodAnalyzerContext
        {
            public readonly List<IDeclaration> HotMethods = new List<IDeclaration>();

            // Current depth of analysis
            public int Depth { get; set; }

            // Inverted call graph for costly reachable mark propagation after visit AST
            public readonly Dictionary<IDeclaredElement, HashSet<IDeclaredElement>> InvertedCallGraph = new Dictionary<IDeclaredElement, HashSet<IDeclaredElement>>();

            // Container of all invoked method for specified method with node elements. Helper for highlighting after
            // propagation of costly reachable mark is done
            public readonly Dictionary<IDeclaredElement, Dictionary<IDeclaredElement, List<IInvocationExpression>>> InvocationsInMethods
                = new Dictionary<IDeclaredElement, Dictionary<IDeclaredElement, List<IInvocationExpression>>>();

            // Visited nodes
            private readonly ISet<IDeclaredElement> myVisited = new HashSet<IDeclaredElement>();

            // Helper for answer question : Is declared element has costly reachable mark?
            public readonly HashSet<IDeclaredElement> CostlyMethods = new HashSet<IDeclaredElement>();


            public IDeclaredElement CurrentDeclaredElement { get; set; }
            public IDeclaration CurrentDeclaration { get;  set; }


            public bool IsCurrentElementVisited()
            {
                return myVisited.Contains(CurrentDeclaredElement);
            }

            public void MarkCurrentAsVisited()
            {
                if (!myVisited.Contains(CurrentDeclaredElement))
                    HotMethods.Add(CurrentDeclaration);

                myVisited.Add(CurrentDeclaredElement);
            }

            public void MarkCurrentAsCostly()
            {
                CostlyMethods.Add(CurrentDeclaredElement);
            }

            public void MarkElementAsCostly(IDeclaredElement element)
            {
                CostlyMethods.Add(element);
            }

            public bool IsDeclaredElementCostly(IDeclaredElement element)
            {
                return CostlyMethods.Contains(element);
            }

            public void RegisterInvocationInMethod(IDeclaredElement invokedMethod, IInvocationExpression invocation)
            {
                var method = CurrentDeclaredElement;
                // remember which methods was invoked from `method`. We will highlight them at the end of analysis if they are marked as `costly reachable`
                if (!InvocationsInMethods.ContainsKey(method))
                    InvocationsInMethods[method] = new Dictionary<IDeclaredElement, List<IInvocationExpression>>();

                var invocationsGroupedByDeclaredElement = InvocationsInMethods[method];
                if (!invocationsGroupedByDeclaredElement.ContainsKey(invokedMethod))
                    invocationsGroupedByDeclaredElement[invokedMethod] = new List<IInvocationExpression>();

                var group = invocationsGroupedByDeclaredElement[invokedMethod];
                group.Add(invocation);

                // add edge to inverted call graph
                if (!InvertedCallGraph.ContainsKey(invokedMethod))
                    InvertedCallGraph[invokedMethod] = new HashSet<IDeclaredElement>();
                InvertedCallGraph[invokedMethod].Add(method);
            }
        }
    }
}