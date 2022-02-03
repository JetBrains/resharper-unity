using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages
{
    [SolutionComponent]
    public class PackageValidator
    {
        private readonly ISolution mySolution;
        private readonly UnityVersion myUnityVersion;
        private const string HelpLink = "https://www.jetbrains.com/help/rider/Running_and_Debugging_Unity_Tests.html";

        public PackageValidator(ISolution solution, UnityVersion unityVersion)
        {
            mySolution = solution;
            myUnityVersion = unityVersion;
        }

        public bool HasNonCompatiblePackagesCombination(bool isCoverage, out string message)
        {
            message = string.Empty;

            if (myUnityVersion.ActualVersionForSolution.Value < new Version("2019.2"))
                return false;

            var manifestJsonFile = mySolution.SolutionDirectory.Combine("Packages/manifest.json");
            if (manifestJsonFile.ExistsFile)
            {
                var text = manifestJsonFile.ReadAllText2().Text;
                var packages = ManifestJson.FromJson(text);

                var testFrameworkPackageId = "com.unity.test-framework";
                var riderPackageId = "com.unity.ide.rider";
                var testFrameworkMarketingName = "Test Framework";
                var riderMarketingName = "Rider Editor";

                if (PackageIsMissing(ref message, packages, testFrameworkPackageId, testFrameworkMarketingName))
                    return true;
                if (PackageIsMissing(ref message, packages, riderPackageId, riderMarketingName)) return true;

                var riderPackageVersion = packages[riderPackageId];
                var testFrameworkVersion = packages[testFrameworkPackageId];
                if (IsOldPackage(ref message, riderPackageVersion, riderMarketingName, "1.1.1")) return true;
                if (IsOldPackage(ref message, testFrameworkVersion, testFrameworkMarketingName, "1.1.1")) return true;

                if (isCoverage && packages.ContainsKey(riderPackageId) && packages.ContainsKey(testFrameworkPackageId))
                {
                    // https://youtrack.jetbrains.com/issue/RIDER-35880
                    if (riderPackageVersion != null && riderPackageVersion < new Version("1.2.0")
                                                    && testFrameworkVersion != null &&
                                                    testFrameworkVersion >= new Version("1.1.5"))
                    {
                        message = $"Update {riderMarketingName} package to v.1.2.0 or later in Unity Package Manager. {HelpLink}";
                        return true;
                    }
                }
            }

            // todo: package may be installed locally, ignore this possibility for now
            // var localPackage = mySolution.SolutionDirectory.Combine("Packages/com.unity.ide.rider/package.json");

            return false;
        }

        private static bool PackageIsMissing(ref string message, Dictionary<string, Version> packages, string packageId, string packageMarketingName)
        {
            if (!packages.ContainsKey(packageId))
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