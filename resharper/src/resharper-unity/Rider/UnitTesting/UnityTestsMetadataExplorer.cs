using System.Threading;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.AssemblyToAssemblyResolvers;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.AttributeChecker;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Exploration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityTestsMetadataExplorer : UnitTestExplorerFrom.DotNetArtefacts
    {
        [NotNull] private readonly ILogger myLogger;
        private readonly UnitTestAttributeCache myUnitTestAttributeCache;
        private readonly IUnitTestElementIdFactory myUnitTestElementIdFactory;
        private readonly UnityTestProvider myUnityTestProvider;
        private readonly IUnitTestElementManager myUnitTestElementManager;
        private readonly UnityNUnitServiceProvider myServiceProvider;

        public UnityTestsMetadataExplorer([NotNull] ISolution solution, [NotNull] IUnitTestProvider provider,
            [NotNull] AssemblyToAssemblyReferencesResolveManager resolveManager,
            [NotNull] ResolveContextManager resolveContextManager, [NotNull] ILogger logger,
            UnitTestAttributeCache unitTestAttributeCache, IUnitTestElementIdFactory unitTestElementIdFactory, UnityTestProvider unityTestProvider, IUnitTestElementManager unitTestElementManager, UnityNUnitServiceProvider serviceProvider)
            : base(solution, provider, resolveManager, resolveContextManager, logger)
        {
            myLogger = logger;
            myUnitTestAttributeCache = unitTestAttributeCache;
            myUnitTestElementIdFactory = unitTestElementIdFactory;
            myUnityTestProvider = unityTestProvider;
            myUnitTestElementManager = unitTestElementManager;
            myServiceProvider = serviceProvider;
        }

        public override void ProcessProject(IProject project, FileSystemPath assemblyPath, MetadataLoader loader,
            IUnitTestElementsObserver observer, CancellationToken token)
        {
            MetadataElementsSource.ExploreProject(project, assemblyPath, loader, observer, myLogger, token,
                metadataAssembly =>
                {
                    var exploration = new UnityTestsExploration(myUnitTestAttributeCache, project, observer, myUnitTestElementIdFactory, myUnityTestProvider, myUnitTestElementManager, myServiceProvider);
                    exploration.Explore(metadataAssembly, token);
                });
        }
    }
}