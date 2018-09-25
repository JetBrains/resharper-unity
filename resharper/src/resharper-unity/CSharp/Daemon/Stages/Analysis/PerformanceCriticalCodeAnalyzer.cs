using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(ICSharpFile), HighlightingTypes = new[] { typeof(ExplicitTagStringComparisonWarning) })]
    public class PerformanceCriticalCodeAnalyzer : UnityElementProblemAnalyzer<ICSharpFile>
    {
        private static readonly HashSet<string> ourKnownHotMonoBehaviourMethods = new HashSet<string>()
        {
            "Update", "LateUpdate", "FixedUpdate",
        };


        public PerformanceCriticalCodeAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(ICSharpFile element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var sourceFile = element.GetSourceFile();
            if (sourceFile == null)
                return;

            var descendantsEnumerator = element.Descendants();
            
            // collect classes which inherited from MonoBehaviour
            var monoBehaviourDerivedClasses = new List<IClassDeclaration>();
            while (descendantsEnumerator.MoveNext())
            {
                switch (descendantsEnumerator.Current)
                {
                    case IClassDeclaration classDeclaration:
                        var declaredSymbol = classDeclaration.DeclaredElement;
                        if (declaredSymbol != null)
                        {
                            if(declaredSymbol.GetAllSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.MonoBehaviour)))
                                monoBehaviourDerivedClasses.Add(classDeclaration);
                        }
                        break;
                    case IMethodDeclaration methodDeclaration:
                        descendantsEnumerator.SkipThisNode();
                        break;
                }
            }

            var hotRootMethods = new List<IMethodDeclaration>();

            
            // help map for higlighting invocation is `StartCoroutine`
            var highlightCoroutineCalls = new Dictionary<IDeclaredElement, IReference>();
            foreach (var classDeclaration in monoBehaviourDerivedClasses)
            {
                descendantsEnumerator = classDeclaration.Descendants();
                while (descendantsEnumerator.MoveNext())
                {
                    switch (descendantsEnumerator.Current)
                    {
                        case IClassDeclaration _:
                            descendantsEnumerator.SkipThisNode();
                            break;
                        case IMethodDeclaration methodDeclaration:
                            var name = methodDeclaration.DeclaredElement?.ShortName;
                            if (name != null && ourKnownHotMonoBehaviourMethods.Contains(name))
                                hotRootMethods.Add(methodDeclaration);                                
                            break;
                        case IInvocationExpression invocationExpression:
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
                                            
                                            var declarations = method.GetDeclarationsIn(sourceFile);
                                            declarations.ForEach(t => hotRootMethods.Add((IMethodDeclaration)t));
                                        }
                                    }

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

                                        var declarations = method.GetDeclarationsIn(sourceFile);
                                        declarations.ForEach(t => hotRootMethods.Add((IMethodDeclaration)t));

                                        highlightCoroutineCalls[method] = invocationReference;
                                    }
                                    // find method
                                    
                                }
                            }
                            
                            break;
                    }
                }
            }

            // TODO : parallel here
            var visitedDeclarations = new HashSet<IDeclaredElement>();

            foreach (var methodDeclaration in hotRootMethods)
            {
                var declaredElement = methodDeclaration.DeclaredElement.NotNull("declaredElement != null");
                var context = new HotMethodAnalyzerContext(declaredElement, visitedDeclarations);
                var visitor = new HotMethodAnalyzer(sourceFile, consumer, 10);
                methodDeclaration.ProcessDescendants(visitor, context);
                
                if (context.IsDeclaredElementCostlyReachable(declaredElement))
                {
                    context.MarkCurrentAsCostlyReachable();
                    if (highlightCoroutineCalls.TryGetValue(declaredElement, out var toHighlight))
                    {
                        consumer.AddHighlighting(new CostlyMethodReachableWarning(toHighlight));
                    }
                }
             
                foreach (var awaitHighlighting in context.AwaitList[declaredElement])
                {
                    consumer.AddHighlighting(new CostlyMethodReachableWarning(awaitHighlighting));
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
                if (myMaxDepth <= context.Depth)
                {
                    return;
                }

                var reference = (invocationExpressionParam.InvokedExpression as IReferenceExpression)?.Reference;

                var declaredElement = reference?.Resolve().DeclaredElement as IMethod;
                if (declaredElement == null)
                    return;

                if (context.IsDeclaredElementVisited(declaredElement))
                {
                    // we have already started processing this declaration, but it can be mark in future as costly.
                    // so add to await list.
                    if (context.IsDeclaredElementCostlyReachable(declaredElement))
                    {
                        myConsumer.AddHighlighting(new CostlyMethodReachableWarning(reference));
                    }
                    else
                    {
                        context.AwaitList.Add(declaredElement, reference);
                    }
                    return;
                }
                    
                var declarations = declaredElement.GetDeclarationsIn(mySourceFile);

                var originDeclaredElement = context.CurrentDeclaredElement;
                context.CurrentDeclaredElement = declaredElement;
                foreach (var declaration in declarations)
                {
                    declaration.ProcessDescendants(this, context);
                }

                context.CurrentDeclaredElement = originDeclaredElement;

                if (context.IsDeclaredElementCostlyReachable(declaredElement))
                {
                    myConsumer.AddHighlighting(new CostlyMethodReachableWarning(reference));
                    context.MarkCurrentAsCostlyReachable();
                    foreach (var awaitHighlighting in context.AwaitList[declaredElement])
                    {
                        myConsumer.AddHighlighting(new CostlyMethodReachableWarning(awaitHighlighting));
                    }
                    
                }

                context.AwaitList.RemoveKey(declaredElement);
                context.MarkCurrentAsVisited();
            }

            private void AnalyzeInvocationExpression(IInvocationExpression invocationExpressionParam, HotMethodAnalyzerContext context)
            {
                var reference = invocationExpressionParam.Reference;

                var declaredElement = reference?.Resolve().DeclaredElement as IMethod;
                if (declaredElement == null)
                    return;

                var shortName = declaredElement.ShortName;

                if (shortName.StartsWith("Find"))
                    context.MarkCurrentAsCostlyReachable();
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
                if (element is IInvocationExpression)
                    context.Depth++;
                
                if (element is ICSharpTreeNode node)
                    node.Accept(this, context);
            }

            public void ProcessAfterInterior(ITreeNode element, HotMethodAnalyzerContext context)
            {
                if (element is IInvocationExpression)
                    context.Depth--;
            }
        }

        private class HotMethodAnalyzerContext
        {
            public int Depth { get; set; }

            private ISet<IDeclaredElement> myVisited;
            private HashSet<IDeclaredElement> myCostlyMethods = new HashSet<IDeclaredElement>();
            public OneToListMap<IDeclaredElement, IReference> AwaitList = new OneToListMap<IDeclaredElement, IReference>();
            
            public IDeclaredElement CurrentDeclaredElement { get; set; }
            
            public HotMethodAnalyzerContext(IDeclaredElement currentDeclaredElement, ISet<IDeclaredElement> visitedContainer)
            {
                CurrentDeclaredElement = currentDeclaredElement;
                myVisited = visitedContainer;
            }

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

            public bool IsCurrentDeclaredElementCostlyReachable()
            {
                return myCostlyMethods.Contains(CurrentDeclaredElement);
            }
            
            public bool IsDeclaredElementCostlyReachable(IDeclaredElement element)
            {
                return myCostlyMethods.Contains(element);
            }
        } 
    }
}