using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph
{
    [SolutionComponent]
    public class CallGraphBurstMarksProvider : CallGraphRootMarksProviderBase
    {
        private readonly List<IBurstBannedAnalyzer> myBurstBannedAnalyzers;

        public static CallGraphRootMarksProviderId ProviderId =
            new CallGraphRootMarksProviderId(nameof(CallGraphBurstMarksProvider));

        public CallGraphBurstMarksProvider(Lifetime lifetime, ISolution solution, UnityReferencesTracker referencesTracker,
            UnitySolutionTracker tracker,
            IEnumerable<IBurstBannedAnalyzer> prohibitedContextAnalyzers)
            : base(nameof(CallGraphBurstMarksProvider),
                new CallGraphBurstPropagator(solution, nameof(CallGraphBurstMarksProvider)))
        {
            Enabled.Value = tracker.IsUnityProject.HasTrueValue();
            referencesTracker.HasUnityReference.Advise(lifetime, b => Enabled.Value = Enabled.Value | b);
            myBurstBannedAnalyzers = prohibitedContextAnalyzers.ToList();
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = new HashSet<IDeclaredElement>();
            switch (currentNode)
            {
                case IStructDeclaration structDeclaration
                    when structDeclaration.DeclaredElement is IStruct @struct &&
                         @struct.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self):
                {
                    var superTypes = @struct.GetAllSuperTypes();
                    var interfaces = superTypes
                        .Where(declaredType => declaredType.IsInterfaceType())
                        .Select(declaredType => declaredType.GetTypeElement())
                        .WhereNotNull()
                        .Where(typeElement =>
                            typeElement.HasAttributeInstance(KnownTypes.JobProducerAttrubyte, AttributesSource.Self))
                        .ToList();
                    var structMethods = @struct.Methods.ToList();

                    foreach (var @interface in interfaces)
                    {
                        var interfaceMethods = @interface.Methods.ToList();
                        var overridenMethods = structMethods
                            .Where(m => interfaceMethods.Any(m.OverridesOrImplements))
                            .ToList();

                        foreach (var overridenMethod in overridenMethods)
                            result.Add(overridenMethod);
                    }

                    break;
                }
                case IInvocationExpression invocationExpression:
                {
                    if (!(CallGraphUtil.GetCallee(invocationExpression) is IMethod method))
                        break;
                    var containingType = method.GetContainingType();
                    if (containingType == null)
                        break;
                    if (method.Parameters.Count == 1 &&
                        method.TypeParameters.Count == 1 &&
                        method.ShortName == "CompileFunctionPointer" &&
                        containingType.GetClrName().Equals(KnownTypes.BurstCompiler))
                    {
                        var argumentList = invocationExpression.ArgumentList.Arguments;
                        if (argumentList.Count != 1)
                            break;
                        var argument = argumentList[0].Value;
                        if (argument == null)
                            break;
                        var possibleDeclaredElements = CallGraphUtil.ExtractCallGraphDeclaredElements(argument);
                        foreach (var declaredElement in possibleDeclaredElements)
                        {
                            if (declaredElement != null)
                                result.Add(declaredElement);
                        }
                    }

                    break;
                }
            }

            return new LocalList<IDeclaredElement>(result.WhereNotNull());
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = new LocalList<IDeclaredElement>();
            if (containingFunction == null)
                return result;
            var functionDeclaration = currentNode as IFunctionDeclaration;
            var function = functionDeclaration?.DeclaredElement;
            if (function == null)
                return result;
            if (IsBurstContextBannedForFunction(function) || CheckBurstBannedAnalyzers(functionDeclaration))
                result.Add(function);
            return result;
        }

        private bool CheckBurstBannedAnalyzers(IFunctionDeclaration node)
        {
            var processor = new BurstBannedProcessor(myBurstBannedAnalyzers, node);
            node.ProcessDescendants(processor);
            return processor.ProcessingIsFinished;
        }

        private sealed class BurstBannedProcessor : UnityCallGraphCodeProcessor
        {
            private readonly List<IBurstBannedAnalyzer> myBurstBannedAnalyzers;

            public BurstBannedProcessor(List<IBurstBannedAnalyzer> burstBannedAnalyzers, ITreeNode startNode)
                : base(startNode)
            {
                myBurstBannedAnalyzers = burstBannedAnalyzers;
            }

            public override bool InteriorShouldBeProcessed(ITreeNode element)
            {
                SeldomInterruptChecker.CheckForInterrupt();

                if (element == StartTreeNode)
                    return true;

                return !UnityCallGraphUtil.IsFunctionNode(element) && !IsBurstContextBannedNode(element);
            }

            public override void ProcessBeforeInterior(ITreeNode element)
            {
                foreach (var contextAnalyzer in myBurstBannedAnalyzers)
                {
                    if (!contextAnalyzer.Check(element))
                        continue;

                    ProcessingIsFinished = true;

                    return;
                }
            }
        }
    }
}