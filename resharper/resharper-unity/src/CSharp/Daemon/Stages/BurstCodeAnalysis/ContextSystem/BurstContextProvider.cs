using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public sealed class BurstContextProvider : UnityProblemAnalyzerContextProviderBase
    {
        private static HashSet<IClrTypeName> myJobsSet = new HashSet<IClrTypeName>
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

        public BurstContextProvider(IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, BurstMarksProvider marksProviderBase)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase)
        {
        }

        public override UnityProblemAnalyzerContextElement Context => UnityProblemAnalyzerContextElement.BURST_CONTEXT;

        
        
        /// <summary>
        /// This is fast and incomplete version of <see cref="BurstMarksProvider"/>
        /// </summary>
        /// <param name="declaration"></param>
        /// <returns></returns>
        protected override bool IsRootFast(ICSharpDeclaration declaration)
        {
            var methodDeclaration = declaration as IMethodDeclaration;
            var method = methodDeclaration?.DeclaredElement;
            var methodSignature = method?.GetSignature(EmptySubstitution.INSTANCE);
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

        protected override bool IsProhibitedFast(ICSharpDeclaration declaration) => IsBurstContextBannedNode(declaration);
    }
}