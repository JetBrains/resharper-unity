using JetBrains.Application.Parts;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IBurstBulbItemsProvider : ICallGraphBulbItemsProvider
    {
    }
}