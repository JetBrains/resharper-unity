using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
    public class BurstMarksProvider : CallGraphRootMarksProviderBase
    {
        private readonly List<IBurstBannedAnalyzer> myBurstBannedAnalyzers;

        public const string MarkId = "Unity.BurstContext";
        public static CallGraphRootMarksProviderId ProviderId = new CallGraphRootMarksProviderId(MarkId);

        public BurstMarksProvider(Lifetime lifetime, ISolution solution,
            UnityReferencesTracker referencesTracker,
            UnitySolutionTracker tracker,
            IEnumerable<IBurstBannedAnalyzer> prohibitedContextAnalyzers)
            : base(MarkId, new BurstPropagator(solution, MarkId))
        {
            Enabled.Value = tracker.IsUnityProject.HasTrueValue();
            referencesTracker.HasUnityReference.Advise(lifetime, b => Enabled.Value = Enabled.Value | b);
            myBurstBannedAnalyzers = prohibitedContextAnalyzers.ToList();
        }

        private static LocalList<IDeclaredElement> GetMarksFromStruct([NotNull] IStruct @struct)
        {
            var result = new LocalList<IDeclaredElement>();
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

            if (interfaces.Count == 0)
                return result;

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

            return result;
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = new LocalList<IDeclaredElement>();

            switch (currentNode)
            {
                case IFunctionDeclaration functionDeclaration when containingFunction != null:
                {
                    if (UnityCallGraphUtil.HasAnalysisComment(functionDeclaration,
                        MarkId, ReSharperControlConstruct.Kind.Restore))
                        result.Add(functionDeclaration.DeclaredElement);

                    break;
                }
                case IClassLikeDeclaration classLikeDeclaration:
                {
                    var typeElement = classLikeDeclaration.DeclaredElement;

                    if (typeElement == null
                        || !typeElement.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                        break;

                    if (typeElement is IStruct @struct)
                        result = GetMarksFromStruct(@struct);

                    foreach (var method in typeElement.Methods)
                    {
                        if (method.IsStatic
                            && method.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                            result.Add(method);
                    }

                    break;
                }
            }

            return result;
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

            if (IsBurstProhibitedFunction(function) || CheckBurstBannedAnalyzers(functionDeclaration))
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

                return !UnityCallGraphUtil.IsFunctionNode(element) && !IsBurstProhibitedNode(element);
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