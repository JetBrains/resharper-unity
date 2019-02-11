using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityReferenceSynchronizer
    {
        public UnityReferenceSynchronizer(Lifetime lifetime, UnityHost host, UnityReferencesTracker referencesTracker)
        {
            host.PerformModelAction(m =>
            {
                referencesTracker.HasUnityReference.Advise(lifetime, res => { m.HasUnityReference.SetValue(res); });
            });
        }
    }
}