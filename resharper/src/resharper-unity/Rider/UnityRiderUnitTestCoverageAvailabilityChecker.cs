using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.ReSharper.Host.Features.UnitTesting;
using JetBrains.ReSharper.UnitTestFramework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{    
    [ShellComponent]
    public class UnityRiderUnitTestCoverageAvailabilityChecker : IRiderUnitTestCoverageAvailabilityChecker, IHideImplementation<DefaultRiderUnitTestCoverageAvailabilityChecker>
    {
        // this method should be very fast as it gets called a lot
        public HostProviderAvailability GetAvailability(IUnitTestElement element)
        {
            return element.Id.Project.GetSolution().IsUnitySolution()
                ? HostProviderAvailability.Nonexistent
                : HostProviderAvailability.Available;
        }
    }
}