using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Packages
{
    [SolutionComponent]
    public class PackageValidator
    {
        private readonly ISolution mySolution;
        private readonly UnityVersion myUnityVersion;

        public PackageValidator(ISolution solution, UnityVersion unityVersion)
        {
            mySolution = solution;
            myUnityVersion = unityVersion;
        }
        
        public bool HasNonCompatiblePackagesCombination(out string message)
        {
            message = string.Empty;
            
            if (myUnityVersion.GetActualVersionForSolution() < new Version("2019.2"))
                return false;
            
            var manifestJsonFile = mySolution.SolutionDirectory.Combine("Packages/manifest.json");
            if (manifestJsonFile.ExistsFile)
            {
                var text = manifestJsonFile.ReadAllText2().Text;
                var packages = ManifestJson.FromJson(text);

                var testFrameworkPackageName = "com.unity.test-framework";
                var riderPackageName = "com.unity.ide.rider";
                
                if (!packages.ContainsKey(testFrameworkPackageName))
                {
                    message = "Test Framework package is missing";
                    return true;
                }
                
                if (!packages.ContainsKey(riderPackageName))
                {
                    message = "Rider Editor package is missing";
                    return true;
                }

                var testFrameworkVersion = packages[testFrameworkPackageName];
                if (testFrameworkVersion != null && testFrameworkVersion < new Version("1.1.1"))
                {
                    message = "Please update Test Framework package to v.1.1.1+";
                    return true;
                }
                
                if (packages.ContainsKey(riderPackageName) && packages.ContainsKey(testFrameworkPackageName))
                {
                    // https://youtrack.jetbrains.com/issue/RIDER-35880
                    var riderPackageVersion = packages[riderPackageName];
                    if (riderPackageVersion != null && riderPackageVersion < new Version("1.2.0") 
                                                    && testFrameworkVersion!=null && testFrameworkVersion >= new Version("1.1.5"))
                    {
                        message =
                            "Please update Rider package to v.1.2.0+. [Learn more](Analyzing_Coverage_Unity.html)";
                        return true;
                    }
                }
            }

            // todo: package may be installed locally, ignore this possibility for now
            // var localPackage = mySolution.SolutionDirectory.Combine("Packages/com.unity.ide.rider/package.json");

            return false;
        }

    }
}