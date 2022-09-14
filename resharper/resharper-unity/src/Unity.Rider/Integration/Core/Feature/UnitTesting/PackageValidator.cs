using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting
{
    [SolutionComponent]
    public class PackageValidator
    {
        private readonly UnityVersion myUnityVersion;
        private readonly PackageManager myPackageManager;
        private const string HelpLink = "https://www.jetbrains.com/help/rider/Running_and_Debugging_Unity_Tests.html";

        public PackageValidator(UnityVersion unityVersion, PackageManager packageManager)
        {
            myUnityVersion = unityVersion;
            myPackageManager = packageManager;
        }

        public bool HasNonCompatiblePackagesCombination(bool isCoverage, out string message)
        {
            message = string.Empty;

            if (myUnityVersion.ActualVersionForSolution.Value < new Version("2019.2"))
                return false;

            var testFrameworkPackageId = "com.unity.test-framework";
            var riderPackageId = "com.unity.ide.rider";
            var testFrameworkMarketingName = "Test Framework";
            var riderMarketingName = "JetBrains Rider Editor";

            var riderPackage = myPackageManager.GetPackageById(riderPackageId);
            var testFrameworkPackage = myPackageManager.GetPackageById(testFrameworkPackageId);
            if (PackageIsMissing(ref message, riderPackage, riderMarketingName)) 
                return true;
            
            if (PackageIsMissing(ref message, testFrameworkPackage, testFrameworkMarketingName)) 
                return true;

            if (riderPackage != null && testFrameworkPackage != null)
            {
                var riderPackageVersion = new Version(riderPackage.PackageDetails.Version);
                var testFrameworkVersion = new Version(testFrameworkPackage.PackageDetails.Version);
                if (IsOldPackage(ref message, riderPackageVersion, riderMarketingName, "1.1.1")) return true;
                if (IsOldPackage(ref message, testFrameworkVersion, testFrameworkMarketingName, "1.1.1")) return true;

                if (isCoverage)
                {
                    // https://youtrack.jetbrains.com/issue/RIDER-35880
                    if (riderPackageVersion < new Version("1.2.0") &&
                             testFrameworkVersion >= new Version("1.1.5"))
                    {
                        message = $"Update {riderMarketingName} package to v.1.2.0 or later in Unity Package Manager. {HelpLink}";
                        return true;
                    }
                }
            }
            
            return false;
        }

        private bool PackageIsMissing(ref string message, [CanBeNull] PackageData packageData, string packageMarketingName)
        {
            if (packageData == null)
            {
                message = $"Add {packageMarketingName} in Unity Package Manager. {HelpLink}";
                return true;
            }

            return false;
        }

        private static bool IsOldPackage(ref string message, Version packageVersion, string packageMarketingName, string targetVersion)
        {
            if (packageVersion != null && packageVersion < new Version(targetVersion))
            {
                message = $"Update {packageMarketingName} package to v.{targetVersion} or later in Unity Package Manager. {HelpLink}";
                return true;
            }

            return false;
        }

        public bool CanRunPlayModeTests(out string message)
        {
            message = string.Empty;
            if (myUnityVersion.ActualVersionForSolution.Value >= new Version("2019.2"))
                return true;

            message = $"Unity 2019.2 or later is required to run play-mode tests. {HelpLink}";
            return false;
        }
    }
}