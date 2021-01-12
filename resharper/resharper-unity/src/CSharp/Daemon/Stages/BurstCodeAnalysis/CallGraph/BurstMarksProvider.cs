using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
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

        public const string MarkId = "Unity.BurstContext";

        public BurstMarksProvider(Lifetime lifetime, ISolution solution,
            UnityReferencesTracker referencesTracker,
            UnitySolutionTracker tracker,
            BurstStrictlyBannedMarkProvider strictlyBannedMarkProvider,
            IEnumerable<IBurstBannedAnalyzer> prohibitedContextAnalyzers)
            : base(MarkId, MarkId, new BurstPropagator(solution, MarkId))
        {
            myBurstBannedAnalyzers = prohibitedContextAnalyzers;
            myStrictlyBannedMarkProvider = strictlyBannedMarkProvider;
            Enabled.Value = tracker.IsUnityProject.HasTrueValue();
            referencesTracker.HasUnityReference.Advise(lifetime, b => Enabled.Value = Enabled.Value | b);
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

            if (!(currentNode is IClassLikeDeclaration classLikeDeclaration))
                return result;
            
            var typeElement = classLikeDeclaration.DeclaredElement;

            if (typeElement == null
                || !typeElement.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                return result;

            if (typeElement is IStruct @struct)
                AddMarksFromStruct(@struct, ref result);

            foreach (var method in typeElement.Methods)
            {
                if (method.IsStatic
                    && method.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                    result.Add(method);
            }

            return result;
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
                SeldomInterruptChecker.CheckForInterrupt();

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