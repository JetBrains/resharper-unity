using System;
using System.Linq;
using JetBrains.Util.DevEnv;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Utils
{
    public abstract class VsUnityWorkloadDetector
    {
        // https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/getting-started-with-visual-studio-tools-for-unity?pivots=windows
        // Workload marketing name: "Game development with Unity".
        private const string VsUnityWorkloadName = "Microsoft.VisualStudio.Workload.ManagedGame";

        private readonly IVsEnvironmentStaticInformation myVsEnvironment;

        protected VsUnityWorkloadDetector(IVsEnvironmentStaticInformation vsEnvironment)
        {
            myVsEnvironment = vsEnvironment;
        }

        public bool IsUnityWorkloadInstalled() => IsUnityWorkloadInstalled(myVsEnvironment);

        public static bool IsUnityWorkloadInstalled(IVsEnvironmentStaticInformation vsEnvironment)
        {
            if (DevenvHostDiscovery.ShouldIgnoreDetectedWorkloads())
                return true;

            if (vsEnvironment.InstalledWorkloads.IsDefault)
                return true;  // We don't know installed VS workloads, enable the plugin

            return vsEnvironment.InstalledWorkloads.Any(name => name.StartsWith(VsUnityWorkloadName, StringComparison.OrdinalIgnoreCase));
        }
    }
}