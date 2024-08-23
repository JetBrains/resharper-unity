using System;
using JetBrains.Application;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Execution.Hosting;
using JetBrains.Rider.Backend.Features.UnitTesting;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting
{
    [ShellComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class UnityRiderUnitTestCoverageAvailabilityChecker : IRiderUnitTestCoverageAvailabilityChecker, IHideImplementation<DefaultRiderUnitTestCoverageAvailabilityChecker>
    {
        private static readonly Version ourMinSupportedUnityVersion = new(2019, 2);

        // this method should be very fast as it gets called a lot
        public HostProviderAvailability GetAvailability(IUnitTestElement element)
        {
            if (!element.Project.IsUnityProject())
                return HostProviderAvailability.Available;

            var solution = element.Project.GetSolution();
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