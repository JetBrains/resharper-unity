using JetBrains.Application.Parts;
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
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class UnitySerializedReferenceProviderMock : UnitySerializedReferenceProvider
    {
        public UnitySerializedReferenceProviderMock(Lifetime lifetime, IUnityElementIdProvider provider,
            IPsiAssemblyFileLoader psiAssemblyFileLoader, IPsiModules psiModules,
            UnitySolutionTracker unitySolutionTracker,
            SolutionAnalysisConfiguration solutionAnalysisConfiguration,
            ShellCaches shellCaches,
            ISolutionCaches solutionCaches)
            : base(lifetime, provider, psiAssemblyFileLoader, psiModules, unitySolutionTracker, solutionAnalysisConfiguration, shellCaches, solutionCaches)
        {
            Enabled.Value = true;
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