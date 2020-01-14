using System;
using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.UnitTesting;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [ShellComponent]
    public class UnityRiderUnitTestCoverageAvailabilityChecker : IRiderUnitTestCoverageAvailabilityChecker, IHideImplementation<DefaultRiderUnitTestCoverageAvailabilityChecker>
    {
        private static readonly Version ourMinSupportedUnityVersion = new Version(2018, 3);

        // this method should be very fast as it gets called a lot
        public HostProviderAvailability GetAvailability(IUnitTestElement element)
        {
            var solution = element.Id.Project.GetSolution();
            var tracker = solution.GetComponent<UnitySolutionTracker>();
            if (tracker.IsUnityProject.HasValue() && !tracker.IsUnityProject.Value)
                return HostProviderAvailability.Available;

            var rdUnityModel = solution.GetProtocolSolution().GetRdUnityModel();
            switch (rdUnityModel.UnitTestPreference.Value)
            {
                case UnitTestLaunchPreference.NUnit:
                    return HostProviderAvailability.Available;
                case UnitTestLaunchPreference.PlayMode:
                    return HostProviderAvailability.Nonexistent;
                case UnitTestLaunchPreference.EditMode:
                {
                    var unityVersion = UnityVersion.Parse(rdUnityModel.UnityApplicationData.Maybe.ValueOrDefault.ApplicationVersion);

                    return unityVersion == null || unityVersion < ourMinSupportedUnityVersion
                        ? HostProviderAvailability.Nonexistent
                        : HostProviderAvailability.Available;
                }

                default:
                    return HostProviderAvailability.Nonexistent;
            }
        }
    }
}