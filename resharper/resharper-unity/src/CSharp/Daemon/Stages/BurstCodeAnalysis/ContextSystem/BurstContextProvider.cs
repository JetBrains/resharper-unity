using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public sealed class BurstContextProvider : CallGraphContextProviderBase
    {
        private readonly HashSet<IClrTypeName> myJobsSet = new HashSet<IClrTypeName>
        {
            KnownTypes.Job,
            KnownTypes.JobFor,
            KnownTypes.JobComponentSystem,
            KnownTypes.JobParallelFor,
            KnownTypes.JobParticleSystem,
            KnownTypes.JobParallelForTransform,
            KnownTypes.JobParticleSystemParallelFor,
            KnownTypes.JobParticleSystemParallelForBatch,
            KnownTypes.AnimationJob
        };

        private readonly IProperty<bool> myIsBurstEnabledProperty;

        public BurstContextProvider(Lifetime lifetime, IElementIdProvider elementIdProvider, IApplicationWideContextBoundSettingStore store,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, BurstMarksProvider marksProviderBase, SolutionAnalysisService service)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase, service)
        {
            myIsBurstEnabledProperty = store.BoundSettingsStore.GetValueProperty(lifetime, (UnitySettings key) => key.EnableBurstCodeHighlighting);
        }

        public override CallGraphContextElement Context => CallGraphContextElement.BURST_CONTEXT;
        public override bool IsContextAvailable => myIsBurstEnabledProperty.Value;
        public override bool IsContextChangingNode(ITreeNode node) => IsBurstProhibitedNode(node) || base.IsContextChangingNode(node);

        protected override bool CheckDeclaration(IDeclaration declaration, out bool isMarked)
        {
            if (IsBurstProhibitedNode(declaration))
            {
                isMarked = false;
                return true;
            }

            return base.CheckDeclaration(declaration, out isMarked);
        }

        protected override bool CheckDeclaredElement(IDeclaredElement element, out bool isMarked)
        {
            if (IsMarkedFast(element))
            {
                isMarked = true;
                return true;
            }

            if (IsBannedFast(element))
            {
                isMarked = false;
                return true;
            }

            return base.CheckDeclaredElement(element, out isMarked);
        }

        private bool IsMarkedFast(IDeclaredElement declaredElement)
        {
            var method = declaredElement as IMethod;

            var containingTypeElement = method?.GetContainingType();

            if (containingTypeElement == null)
                return false;

            if (!containingTypeElement.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                return false;

            if (method.IsStatic && method.HasAttributeInstance(KnownTypes.BurstCompileAttribute, AttributesSource.Self))
                return true;

            if (!(containingTypeElement is IStruct @struct))
                return false;

            var superTypes = @struct.GetSuperTypes();
            var methodSignature = method.GetSignature(EmptySubstitution.INSTANCE);

            foreach (var type in superTypes)
            {
                var clrName = type.GetClrName();

                if (!myJobsSet.Contains(clrName))
                    continue;

                var typeElement = type.GetTypeElement();

                if (typeElement == null)
                    continue;

                var typeMethods = typeElement.Methods;

                foreach (var typeMethod in typeMethods)
                {
                    var typeMethodSignature = typeMethod.GetSignature(EmptySubstitution.INSTANCE);

                    if (methodSignature.Equals(typeMethodSignature))
                        return true;
                }
            }

            return false;
        }

        private static bool IsBannedFast(IDeclaredElement declaredElement)
        {
            return declaredElement is IFunction function && IsBurstProhibitedFunction(function);
        }
    }
}