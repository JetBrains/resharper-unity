using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Utils;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Context
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnitySolutionInformation: IUnitySolutionInformation
    {
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ISolution mySolution;
        private readonly PackageManager myPackageManager;

        public UnitySolutionInformation(UnitySolutionTracker solutionTracker, ISolution solution, PackageManager packageManager)
        {
            mySolutionTracker = solutionTracker;
            mySolution = solution;
            myPackageManager = packageManager;
        }
        
        public bool IsUnitySolution()
        {
            return mySolutionTracker.IsUnityProjectFolder.HasTrueValue();
        }

        public string GetUnityVersion()
        {
            var (verifiedVersion, isCustom) = UnityVersionUtils.GetUnityVersion(UnityVersion.GetProjectSettingsUnityVersion(mySolution.SolutionDirectory));
            return verifiedVersion;
        }

        public IEnumerable<string> GetPackages()
        {
            return myPackageManager.Packages.Select(t => t.Value.Id);
        }
    }
}