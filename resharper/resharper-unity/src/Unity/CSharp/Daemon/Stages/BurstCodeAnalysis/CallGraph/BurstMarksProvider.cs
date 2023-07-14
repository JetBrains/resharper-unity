using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph
{
    [SolutionComponent]
    public class BurstMarksProvider : CallGraphCommentMarksProvider
    {
        private readonly IEnumerable<IBurstBannedAnalyzer> myBurstBannedAnalyzers;
        private readonly BurstStrictlyBannedMarkProvider myStrictlyBannedMarkProvider;

        private static readonly HashSet<string> ourSystemBurstableMethods = new()
            { "OnCreate", "OnStartRunning", "OnUpdate", "OnStopRunning", "OnDestroy", "OnCreateForCompiler" };

        public const string MarkId = "Unity.BurstContext";

        public BurstMarksProvider(Lifetime lifetime, ISolution solution,
            UnitySolutionTracker tracker,
            BurstStrictlyBannedMarkProvider strictlyBannedMarkProvider,
            IEnumerable<IBurstBannedAnalyzer> prohibitedContextAnalyzers)
            : base(MarkId, MarkId, new BurstPropagator(solution, MarkId))
        {
            myBurstBannedAnalyzers = prohibitedContextAnalyzers;
            myStrictlyBannedMarkProvider = strictlyBannedMarkProvider;
            Enabled.Value = tracker.IsUnityProject.HasTrueValue();
            tracker.HasUnityReference.Advise(lifetime, b => Enabled.Value = Enabled.Value | b);
        }

        private static void AddMarksFromStruct([NotNull] IStruct @struct, ref LocalList<IDeclaredElement> result)
        {
            var superTypes = @struct.GetAllSuperTypes();
            var interfaces = new LocalList<ITypeElement>();

            foreach (var super in superTypes)
            {
                if (!super.IsInterfaceType())
                    continue;

                var superElement = super.GetTypeElement();

                if (superElement == null)
                    continue;

                if (superElement.HasAttributeInstance(KnownTypes.JobProducerAttribute, AttributesSource.Self))
                    interfaces.Add(superElement);
            }

            if (interfaces.Count == 0) return;

            var canBeOverridenByInterfaceMethods = new LocalList<IMethod>();

            foreach (var method in @struct.Methods)
            {
                if (!method.IsOverride && method.HasImmediateSuperMembers())
                    canBeOverridenByInterfaceMethods.Add(method);
            }

            foreach (var @interface in interfaces)
            {
                foreach (var interfaceMethod in @interface.Methods)
                {
                    foreach (var structMethod in canBeOverridenByInterfaceMethods)
                    {
                        if (structMethod.OverridesOrImplements(interfaceMethod))
                            result.Add(structMethod);
                    }
                }
            }
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = base.GetRootMarksFromNode(currentNode, containingFunction);

            //[BurstCompile] by default only works on static functions (where the type itself is also marked BurstCompile).
            //For jobs (Any struct implementing an interface that has the attribute JobProducerType), you just put BurstCompile on the struct.
            ProcessOriginalBurstRules(currentNode, ref result);

            // For ISystem, we check OnCreate, OnStartRunning, OnUpdate, OnStopRunning, OnDestroy and OnCreateForCompiler individually,
            // if any include the attribute, we generate [BurstCompile] on the type itself, and for every one of those function that includes the attribute,
            // we generate a static function inside the type with [BurstCompile] on the function. 
            // That means
            //      1. no need to put BurstCompile on the system struct itself
            //      2. Every one of the 6 functions are individually burst togglable.
            ProcessISystemRules(currentNode, ref result);


            //SystemBase can't be bursted,
            //other than the lambdas from Job.WithCode,
            //and Entities.ForEach which does generate an actual job struct with [BurstCompile] on it
            ProcessSystemBaseLambdas(currentNode, ref result);

            return result;
        }

        private static void ProcessSystemBaseLambdas(ITreeNode currentNode, ref LocalList<IDeclaredElement> result)
        {
            if (currentNode is not IClassLikeDeclaration classLikeDeclaration)
                return;

            var typeElement = classLikeDeclaration.DeclaredElement;

            if (typeElement == null)
                return;

            if (!typeElement.DerivesFrom(KnownTypes.SystemBase))
                return;

            foreach (var methodDeclaration in classLikeDeclaration.MethodDeclarations)
            {
                var burstableLambda = methodDeclaration.Body.FindNextNode(FindBurstableLambdaNode);
                while (burstableLambda is ILambdaExpression lambdaExpression)
                {
                    result.Add(lambdaExpression.DeclaredElement);
                    burstableLambda = burstableLambda.GetContainingNode<IInvocationExpression>()?.NextSibling?.FindNextNode(FindBurstableLambdaNode);
                }
            }
        }

        private static TreeNodeActionType FindBurstableLambdaNode(ITreeNode node)
        {
            if (node is IMethodDeclaration)
                return TreeNodeActionType.IGNORE_SUBTREE;
            if (node is not ILambdaExpression lambdaExpression)
                return TreeNodeActionType.CONTINUE;

            var invocationExpression = lambdaExpression.GetContainingNode<IInvocationExpression>();
            if (invocationExpression.IsJobWithCodeMethod() ||
                invocationExpression.IsEntitiesForEach())
                return TreeNodeActionType.ACCEPT;

            return TreeNodeActionType.CONTINUE;
        }


        private static void ProcessISystemRules(ITreeNode currentNode, ref LocalList<IDeclaredElement> result)
        {
            if (currentNode is not IClassLikeDeclaration classLikeDeclaration)
                return;

            var typeElement = classLikeDeclaration.DeclaredElement;

            if (typeElement == null)
                return;

            if (!typeElement.DerivesFrom(KnownTypes.ISystem))
                return;

            foreach (var method in typeElement.Methods)
            {
                if (!ourSystemBurstableMethods.Contains(method.ShortName))
                    continue;
                if (method.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                    result.Add(method);
            }
        }

        private static void ProcessOriginalBurstRules(ITreeNode currentNode, ref LocalList<IDeclaredElement> result)
        {
            if (currentNode is not IClassLikeDeclaration classLikeDeclaration)
                return;

            var typeElement = classLikeDeclaration.DeclaredElement;

            if (typeElement == null)
                return;

            if (!typeElement.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                return;

            if (typeElement is IStruct @struct)
                AddMarksFromStruct(@struct, ref result);

            foreach (var method in typeElement.Methods)
            {
                if (method.IsStatic
                    && method.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                    result.Add(method);
            }
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = myStrictlyBannedMarkProvider.GetBanMarksFromNode(currentNode, containingFunction);

            if (containingFunction == null)
                return result;

            var functionDeclaration = currentNode as IFunctionDeclaration;
            var function = functionDeclaration?.DeclaredElement;

            if (function == null)
                return result;

            if (CheckBurstBannedAnalyzers(functionDeclaration))
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
            private readonly IEnumerable<IBurstBannedAnalyzer> myBurstBannedAnalyzers;

            public BurstBannedProcessor(IEnumerable<IBurstBannedAnalyzer> burstBannedAnalyzers, ITreeNode startNode)
                : base(startNode)
            {
                myBurstBannedAnalyzers = burstBannedAnalyzers;
            }

            public override bool InteriorShouldBeProcessed(ITreeNode element)
            {
                Interruption.Current.CheckAndThrow();

                if (element == StartTreeNode)
                    return true;

                return !UnityCallGraphUtil.IsFunctionNode(element) && !BurstCodeAnalysisUtil.IsBurstProhibitedNode(element);
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