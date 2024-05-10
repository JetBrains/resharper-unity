using System;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages
{
    [SolutionComponent(Instantiation.DemandAnyThread)]
    public class PackageCompatibilityValidator
    {
        private readonly UnityVersion myUnityVersion;
        private readonly PackageManager myPackageManager;
        private const string HelpLink = "https://www.jetbrains.com/help/rider/Running_and_Debugging_Unity_Tests.html";
        private const string TestFrameworkPackageId = "com.unity.test-framework";
        public static readonly string RiderPackageId = "com.unity.ide.rider";
        private const string TestFrameworkMarketingName = "Test Framework";
        private const string RiderMarketingName = "JetBrains Rider Editor";

        public PackageCompatibilityValidator(UnityVersion unityVersion, PackageManager packageManager)
        {
            myUnityVersion = unityVersion;
            myPackageManager = packageManager;
        }

        public bool HasNonCompatiblePackagesCombination(bool isCoverage, out string message)
        {
            message = string.Empty;

            if (myUnityVersion.ActualVersionForSolution.Value < new Version("2019.2"))
                return false;

            var riderPackage = myPackageManager.GetPackageById(RiderPackageId);
            if (riderPackage == null)
            {
                message = $"Add {RiderMarketingName} in Unity Package Manager.";
                return true;
            }
            
            var testFrameworkPackage = myPackageManager.GetPackageById(TestFrameworkPackageId);            
            if (testFrameworkPackage == null)
            {
                message = $"Add {TestFrameworkMarketingName} in Unity Package Manager. {HelpLink}";
                return true;
            }

            if (JetSemanticVersion.TryParse(riderPackage.PackageDetails.Version, out var riderPackageVersion)
                && JetSemanticVersion.TryParse(testFrameworkPackage.PackageDetails.Version, out var testFrameworkVersion))
            {
                if (IsOldPackage(out message, riderPackageVersion, RiderMarketingName, "1.1.1")) return true;
                if (IsOldPackage(out message, testFrameworkVersion, TestFrameworkMarketingName, "1.1.1")) return true;

                if (isCoverage)
                {
                    // https://youtrack.jetbrains.com/issue/RIDER-35880
                    if (riderPackageVersion < new JetSemanticVersion(1, 2, 0) &&
                        testFrameworkVersion >= new JetSemanticVersion(1, 1, 5))
                    {
                        message = $"Update {RiderMarketingName} package to v.1.2.0 or later in Unity Package Manager. {HelpLink}";
                        return true;
                    }
                }

            }
            
            return false;
        }

        private static bool IsOldPackage(out string message, JetSemanticVersion packageVersion, string packageMarketingName, string targetVersion)
        {
            message = string.Empty;
            if (packageVersion != null && packageVersion < JetSemanticVersion.Parse(targetVersion))
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