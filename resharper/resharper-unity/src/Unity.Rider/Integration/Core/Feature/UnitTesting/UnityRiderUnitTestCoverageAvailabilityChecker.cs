using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Execution.Hosting;
using JetBrains.Rider.Backend.Features.UnitTesting;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting;

[ShellComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityRiderUnitTestCoverageAvailabilityChecker :
    IRiderUnitTestCoverageAvailabilityChecker,
    IHideImplementation<DefaultRiderUnitTestCoverageAvailabilityChecker>
{
    public HostProviderAvailability GetAvailability(IUnitTestElement element)
    {
        if (!element.Project.IsUnityProject())
            return HostProviderAvailability.Available;

        return HostProviderAvailability.Nonexistent;
    }
}