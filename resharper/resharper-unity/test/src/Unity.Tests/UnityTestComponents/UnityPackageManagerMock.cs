using System;
using System.Collections.Generic;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    public static class UnityPackageCookie
    {
        public static IDisposable RunUnityPackageCookie(ISolution solution, string packageName)
        {
            var unityPackageManagerMock = solution.GetComponent<UnityPackageManagerMock>();
            unityPackageManagerMock.RegisterPackage(packageName);
            return Disposable.CreateAction(() => unityPackageManagerMock.UnregisterPackage(packageName));
        }
    }
    
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityPackageManagerMock : PackageManager
    {
        private readonly HashSet<string> myInternalPackages = new();

        public UnityPackageManagerMock(Lifetime lifetime, ISolution solution, ILogger logger,
            UnitySolutionTracker unitySolutionTracker, Plugins.Unity.UnityEditorIntegration.UnityVersion unityVersion,
            IFileSystemTracker fileSystemTracker) : base(lifetime, solution, logger, unitySolutionTracker,
            unityVersion, fileSystemTracker)
        {
        }

        public void RegisterPackage(string packageName) => myInternalPackages.Add(packageName);
        public void UnregisterPackage(string packageName) => myInternalPackages.Remove(packageName);

        public override PackageData? GetPackageById(string id) => base.GetPackageById(id) ?? GetPackageByIdInternal(id);

        private PackageData? GetPackageByIdInternal(string id)
        {
            if (myInternalPackages.Contains(id))
            {
                return new PackageData(id, null, DateTime.Today, new PackageDetails(id, id, String.Empty,
                        "Tests Mock Package", string.Empty, new Dictionary<string, string>())
                    , PackageSource.Unknown, null, null);
            }

            return null;
        }
    }
}