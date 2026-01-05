using System.Threading;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.AssemblyToAssemblyResolvers;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ProjectModel.NuGet.Packaging;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework.Exploration;
using JetBrains.ReSharper.UnitTestFramework.Exploration.Artifacts;
using JetBrains.ReSharper.UnitTestFramework.Exploration.AttributeChecker;
using JetBrains.ReSharper.UnitTestProvider.nUnit.Common;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting
{
    /// <summary>
    /// Unity-specific NUnit test explorer that handles MicrosoftNetTestSdk requirements.
    /// For Unity library projects, a class with a Main method is not generated, so MicrosoftNetTestSdk is not required.
    /// </summary>
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityNUnitTestExplorerFromMetadata : UnitTestExplorerFrom.Metadata.Discoverable
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly IUnitTestAttributeChecker myAttributeChecker;
        private readonly INUnitTypeOrValuePresenterFactory myPresenterFactory;
        private readonly INUnitVersionDetector myNUnitVersionDetector;
        private readonly ILogger myLogger;

        public UnityNUnitTestExplorerFromMetadata(
            [NotNull] NUnitServiceProvider serviceProvider,
            [NotNull] IUnitTestAttributeChecker attributeChecker,
            [NotNull] AssemblyToAssemblyReferencesResolveManager resolveManager,
            [NotNull] ResolveContextManager resolveContextManager,
            [NotNull] NuGetInstalledPackageChecker installedPackageChecker,
            [NotNull] INUnitTypeOrValuePresenterFactory presenterFactory,
            [NotNull] INUnitVersionDetector nUnitVersionDetector,
            [NotNull] UnitySolutionTracker unitySolutionTracker,
            [NotNull] ILogger logger)
            : base(serviceProvider.Provider, resolveManager, resolveContextManager, installedPackageChecker, logger)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myAttributeChecker = attributeChecker;
            myPresenterFactory = presenterFactory;
            myNUnitVersionDetector = nUnitVersionDetector;
            myLogger = logger;
        }

        public override PertinenceResult IsSupported(IProject project, TargetFrameworkId targetFrameworkId)
        {
            // Only apply this explorer for Unity projects with NETStandard
            if (!myUnitySolutionTracker.IsUnityProject.Maybe.ValueOrDefault)
                return PertinenceResult.No("Not a Unity project");
            
            // for cases except NetStandard - regular NUnit would be just fine
            if (!targetFrameworkId.IsNetStandard)
                return PertinenceResult.No("Not a NETStandard project");

            return PertinenceResult.Yes;
        }

        protected override void ProcessProject(
            MetadataLoader loader,
            IUnitTestElementObserver observer,
            CancellationToken token)
        {
            MetadataElementsSource.ExploreProject(observer.Source.Project, observer.Source.Output, loader, myLogger, token, assembly =>
            {
                var elementFactory = new NUnitElementFactory(observer);
                var explorerFactory = new NUnitMetadataExplorerFactory(myNUnitVersionDetector, myLogger);
                var explorer = explorerFactory.GetExplorer(elementFactory, myAttributeChecker, observer, myPresenterFactory);

                explorer.ExploreAssembly(assembly, token);
            });
        }
    }
}
