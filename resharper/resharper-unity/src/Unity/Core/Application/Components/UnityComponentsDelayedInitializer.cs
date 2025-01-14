using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Components;

[SolutionComponent(InstantiationEx.LegacyDefault)]
public class UnityComponentsDelayedInitializer
{
    public UnityComponentsDelayedInitializer(IEnumerable<IUnityLazyComponent> components,
        UnitySolutionTracker solutionTracker, Lifetime lifetime)
    {
        solutionTracker.IsUnityProjectFolder.AdviseUntil(lifetime, res =>
        {
            if (res)
            {
                // enumerating components causes their initialization
                foreach (var component in components)
                {
                }

                return true;
            }

            return false;
        });
    }
}