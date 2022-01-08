using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnityEditorIntegration
{
    [SolutionComponent]
    public class UnityReferenceSynchronizer
    {
        public UnityReferenceSynchronizer(Lifetime lifetime, FrontendBackendHost host, UnityReferencesTracker referencesTracker)
        {
            host.Do(m =>
            {
                referencesTracker.HasUnityReference.Advise(lifetime, res => { m.HasUnityReference.SetValue(res); });
            });
        }
    }
}