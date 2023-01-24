using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent]
    public class UnitySerializedReferenceProviderMock : UnitySerializedReferenceProvider
    {
        public UnitySerializedReferenceProviderMock(Lifetime lifetime, [NotNull] IUnityElementIdProvider provider,
            [NotNull] IPsiAssemblyFileLoader psiAssemblyFileLoader, [NotNull] IPsiModules psiModules,
            [NotNull] UnitySolutionTracker unitySolutionTracker,
            [NotNull] SolutionAnalysisConfiguration solutionAnalysisConfiguration,
            ShellCaches shellCaches,
            ISolutionCaches solutionCaches)
            : base(lifetime, provider, psiAssemblyFileLoader, psiModules, unitySolutionTracker, solutionAnalysisConfiguration, shellCaches, solutionCaches)
        {
        }

        protected override bool IsUnitySolution => true;

        protected override bool IsValidAssembly(IPsiAssembly assembly)
        {
            if (assembly.AssemblyName.Name.Contains("GenericClassesLib03")
                || assembly.AssemblyName.Name.Contains("UnityRelatedClasses")
                || assembly.AssemblyName.Name.Contains("AssemblyWithSerializedRef")
                || assembly.AssemblyName.Name.Contains("AssemblyWithoutSerializedRef")
                || assembly.AssemblyName.Name.Contains("PropertyWithBackingField")
                || assembly.AssemblyName.Name.Contains("ListArrayFixedBufferTest")
                    )
                return true;
            return false;
        }
    }
}