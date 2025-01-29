using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.Threading;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Components;

public interface IUnityProjectFolderLazyComponent;
public interface IUnityProjectLazyComponent;

[SolutionComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class UnityComponentsDelayedInitializer
{
    public UnityComponentsDelayedInitializer(
        IComponentContainer componentContainer,
        UnitySolutionTracker solutionTracker, Lifetime lifetime)
    {
        solutionTracker.IsUnityProjectFolder.AdviseUntil(lifetime, res =>
        {
            if (res)
            {
                componentContainer.GetComponentsAsync<IUnityProjectFolderLazyComponent>(lifetime).AsTask().NoAwait();
                return true;
            }

            return false;
        });
        
        solutionTracker.IsUnityProject.AdviseUntil(lifetime, res =>
        {
            if (res)
            {
                componentContainer.GetComponentsAsync<IUnityProjectLazyComponent>(lifetime).AsTask().NoAwait();
                return true;
            }

            return false;
        });
    }
}