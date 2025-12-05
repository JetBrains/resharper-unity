using System.Linq;
using JetBrains.Util;
using JetBrains.Util.DevEnv;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Utils
{
    public abstract class VsUnityWorkloadDetector
    {
        // https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/getting-started-with-visual-studio-tools-for-unity?pivots=windows
        // Workload marketing name: "Game development with Unity".
        private const string VsUnityWorkloadName = "Microsoft.VisualStudio.Workload.ManagedGame";

        public static bool IsUnityWorkloadInstalled()
        {
            var instance = DevenvHostDiscovery.TryGetCurrentInstanceSinceVs15(OnError.Ignore);
            if (instance == null || instance.PackagesIfKnown == null)
                return false;

            return instance.PackagesIfKnown.Any(pkg => pkg.Type == DevenvHostDiscovery.InstalledVsPackage.WellKnownTypes.Workload && pkg.Id == VsUnityWorkloadName);
        }
    }
}
