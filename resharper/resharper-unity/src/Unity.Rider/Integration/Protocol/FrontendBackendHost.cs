using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Env;
using JetBrains.RdBackend.Common.Features;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol
{
    [SolutionComponent]
    public class FrontendBackendHost : IFrontendBackendHost
    {
        private readonly bool myIsInTests;

        // This will only ever be null when running tests. The value does not change for the lifetime of the solution.
        // Prefer using this field over calling GetFrontendBackendModel(), as that method will throw in tests
        [CanBeNull] public readonly FrontendBackendModel Model;

        public FrontendBackendHost(Lifetime lifetime, ISolution solution, IShellLocks shellLocks,
                                   PackageManager packageManager,
                                   DeferredCacheController deferredCacheController,
                                   bool isInTests = false)
        {
            myIsInTests = isInTests;
            if (myIsInTests)
                return;

            // This will throw in tests, as GetProtocolSolution will return null
            var model = solution.GetProtocolSolution().GetFrontendBackendModel();
            AdviseModel(lifetime, model, packageManager, deferredCacheController, shellLocks);
            Model = model;
        }

        public bool IsAvailable => !myIsInTests && Model != null;

        // Convenience method to fire and forget an action on the model (e.g. set a value, fire a signal, etc). Fire and
        // forget means it's safe to use during testing, when there won't be a frontend model available, and Model will
        // be null.
        // There is not a Do that takes in a Func to return a value, as that cannot be called reliably in tests. Use
        // Model directly in this case, check for null and do whatever is appropriate for the callsite.
        public void Do(Action<FrontendBackendModel> action)
        {
            if (myIsInTests)
                return;

            action(Model);
        }

        private static void AdviseModel(Lifetime lifetime,
                                        FrontendBackendModel frontendBackendModel,
                                        PackageManager packageManager,
                                        DeferredCacheController deferredCacheController,
                                        IThreading shellLocks)
        {
            AdvisePackages(lifetime, frontendBackendModel, packageManager);
            AdviseIntegrationTestHelpers(lifetime, frontendBackendModel, deferredCacheController, shellLocks);
        }

        private static void AdvisePackages(Lifetime lifetime,
                                           FrontendBackendModel frontendBackendModel,
                                           PackageManager packageManager)
        {
            packageManager.Updating.FlowInto(lifetime, frontendBackendModel.PackagesUpdating);

            // Called in the Guarded reentrancy context
            packageManager.Packages.AddRemove.Advise(lifetime, args =>
            {
                switch (args.Action)
                {
                    case AddRemove.Add:
                        var packageData = args.Value.Value;
                        var packageDetails = packageData.PackageDetails;
                        var source = ToProtocolPackageSource(packageData.Source);
                        var dependencies = (from d in packageDetails.Dependencies
                            select new UnityPackageDependency(d.Key, d.Value)).ToArray();
                        var gitDetails = packageData.GitDetails != null
                            ? new UnityGitDetails(packageData.GitDetails.Url, packageData.GitDetails.Hash,
                                packageData.GitDetails.Revision)
                            : null;
                        var package = new UnityPackage(args.Value.Key, packageDetails.Version,
                            packageData.PackageFolder?.FullPath, source, packageDetails.DisplayName,
                            packageDetails.Description, dependencies, packageData.TarballLocation?.FullPath,
                            gitDetails);
                        frontendBackendModel.Packages.Add(args.Value.Key, package);
                        break;

                    case AddRemove.Remove:
                        frontendBackendModel.Packages.Remove(args.Value.Key);
                        break;
                }
            });
        }

        private static void AdviseIntegrationTestHelpers(Lifetime lifetime, FrontendBackendModel frontendBackendModel,
                                                   DeferredCacheController deferredCacheController,
                                                   IThreading shellLocks)
        {
            deferredCacheController.CompletedOnce.Advise(lifetime, v =>
            {
                if (v)
                {
                    shellLocks.Tasks.StartNew(lifetime, Scheduling.MainDispatcher,
                        () => { frontendBackendModel.IsDeferredCachesCompletedOnce.Value = true; });
                }
            });
        }

        private static UnityPackageSource ToProtocolPackageSource(PackageSource source)
        {
            switch (source)
            {
                case PackageSource.Unknown:         return UnityPackageSource.Unknown;
                case PackageSource.BuiltIn:         return UnityPackageSource.BuiltIn;
                case PackageSource.Registry:        return UnityPackageSource.Registry;
                case PackageSource.Embedded:        return UnityPackageSource.Embedded;
                case PackageSource.Local:           return UnityPackageSource.Local;
                case PackageSource.LocalTarball:    return UnityPackageSource.LocalTarball;
                case PackageSource.Git:             return UnityPackageSource.Git;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }
    }
}