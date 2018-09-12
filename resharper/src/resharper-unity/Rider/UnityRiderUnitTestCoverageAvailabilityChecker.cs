using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.UnitTesting;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{    
    [ShellComponent]
    public class UnityRiderUnitTestCoverageAvailabilityChecker : IRiderUnitTestCoverageAvailabilityChecker, IHideImplementation<DefaultRiderUnitTestCoverageAvailabilityChecker>
    {
        // this method should be very fast as it gets called a lot
        public HostProviderAvailability GetAvailability(IUnitTestElement element)
        {
            var solution = element.Id.Project.GetSolution();
            if (!solution.IsAbleToEstablishProtocolConnectionWithUnity())
                return HostProviderAvailability.Available; 
            
            var rdUnityModel = solution.GetProtocolSolution().GetRdUnityModel();
            if (rdUnityModel.UnitTestPreference.Value == UnitTestLaunchPreference.NUnit)
                return HostProviderAvailability.Available;
            
            return HostProviderAvailability.Nonexistent;
        }
    }
}