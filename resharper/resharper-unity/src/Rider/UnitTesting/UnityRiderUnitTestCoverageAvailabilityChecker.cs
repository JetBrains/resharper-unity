using System;
using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Rider.Backend.Features.UnitTesting;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [ShellComponent]
    public class UnityRiderUnitTestCoverageAvailabilityChecker : IRiderUnitTestCoverageAvailabilityChecker, IHideImplementation<DefaultRiderUnitTestCoverageAvailabilityChecker>
    {
        private static readonly Version ourMinSupportedUnityVersion = new Version(2018, 3);

        // this method should be very fast as it gets called a lot
        public HostProviderAvailability GetAvailability(IUnitTestElement element)
        {
            var solution = element.Project.GetSolution();
            var tracker = solution.GetComponent<UnitySolutionTracker>();
            if (tracker.IsUnityProject.HasValue() && !tracker.IsUnityProject.Value)
                return HostProviderAvailability.Available;

            var frontendBackendModel = solution.GetProtocolSolution().GetFrontendBackendModel();
            switch (frontendBackendModel.UnitTestPreference.Value)
            {
                case UnitTestLaunchPreference.NUnit:
                    return HostProviderAvailability.Available;
                case UnitTestLaunchPreference.Both:
                case UnitTestLaunchPreference.PlayMode:
                case UnitTestLaunchPreference.EditMode:
                {
                    var unityVersion = UnityVersion.Parse(frontendBackendModel.UnityApplicationData.Maybe.ValueOrDefault?.ApplicationVersion ?? string.Empty);

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