using System;
using System.Linq;
using JetBrains.Util.DevEnv;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Utils
{
    public abstract class VsUnityWorkloadDetector
    {
        protected readonly Lazy<bool> IsUnityWorkloadInstalled;

        protected VsUnityWorkloadDetector(IVsEnvironmentStaticInformation vsEnvironment)
        {
            IsUnityWorkloadInstalled = new Lazy<bool>(() => GetIsUnityWorkloadInstalled(vsEnvironment));
        }

        public static bool GetIsUnityWorkloadInstalled(IVsEnvironmentStaticInformation vsEnvironment)
        {
            // https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/getting-started-with-visual-studio-tools-for-unity?pivots=windows
            // Workload marketing name: "Game development with Unity".
            const string VsUnityWorkloadName = "Microsoft.VisualStudio.Workload.ManagedGame";

            if (DevenvHostDiscovery.ShouldIgnoreDetectedWorkloads())
                return true;

            if (vsEnvironment.InstalledWorkloads.IsDefault)
                return true;  // We don't know installed VS workloads, enable the plugin

            return vsEnvironment.InstalledWorkloads.Any(name => name.StartsWith(VsUnityWorkloadName, StringComparison.OrdinalIgnoreCase));
        }
    }
}