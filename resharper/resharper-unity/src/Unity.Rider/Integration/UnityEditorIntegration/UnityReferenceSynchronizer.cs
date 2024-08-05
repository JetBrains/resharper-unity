using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class UnityReferenceSynchronizer
    {
        public UnityReferenceSynchronizer(Lifetime lifetime, FrontendBackendHost host, UnitySolutionTracker unitySolutionTracker)
        {
            host.Do(m =>
            {
                unitySolutionTracker.HasUnityReference.Advise(lifetime, res => { m.HasUnityReference.SetValue(res); });
            });
        }
    }
}