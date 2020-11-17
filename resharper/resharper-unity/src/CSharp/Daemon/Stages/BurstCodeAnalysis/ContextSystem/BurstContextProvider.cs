using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
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
        private static readonly HashSet<IClrTypeName> ourJobsSet = new HashSet<IClrTypeName>
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
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, BurstMarksProvider marksProviderBase)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase)
        {
            myIsBurstEnabledProperty = store.BoundSettingsStore.GetValueProperty(lifetime, (UnitySettings key) => key.EnableBurstCodeHighlighting);
        }

        public override CallGraphContextElement Context => CallGraphContextElement.BURST_CONTEXT;
        public override bool IsContextAvailable => myIsBurstEnabledProperty.Value;
        public override bool IsContextChangingNode(ITreeNode node) => IsBurstProhibitedNode(node) || base.IsContextChangingNode(node);

        public override bool HasContext(IDeclaration declaration, DaemonProcessKind processKind)
        {
            if (IsContextAvailable == false)
                return false;
            
            if (declaration == null || IsBurstProhibitedNode(declaration))
                return false;
            
            var functionDeclaration = declaration as IFunctionDeclaration;

            if (UnityCallGraphUtil.HasAnalysisComment(functionDeclaration,
                BurstMarksProvider.MarkId, ReSharperControlConstruct.Kind.Restore))
                return true;

            return base.HasContext(declaration, processKind);
        }

        public override bool IsMarked(IDeclaredElement declaredElement, DaemonProcessKind processKind)
        {
            if (IsContextAvailable == false)
                return false;
            
            if (IsMarkedFast(declaredElement))
                return true;

            if (IsBannedFast(declaredElement))
                return false;
            
            return base.IsMarked(declaredElement, processKind);
        }

        private static bool IsMarkedFast(IDeclaredElement declaredElement)
        {
            var method = declaredElement as IMethod;

            if (method == null)
                return false;
            
            // if (getCallee && node is ICSharpExpression icSharpExpression)
            //     declaredElement = CallGraphUtil.GetCallee(icSharpExpression);
            var containingTypeElement = method.GetContainingType();

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

                if (!ourJobsSet.Contains(clrName))
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