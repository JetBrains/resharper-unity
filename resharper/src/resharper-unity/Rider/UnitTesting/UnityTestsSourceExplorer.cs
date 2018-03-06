using System;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.AssemblyToAssemblyResolvers;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ReSharper.Feature.Services.ClrLanguages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.AttributeChecker;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Exploration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityTestsSourceExplorer : UnitTestExplorerFrom.DotNetArtefacts, IUnitTestExplorerFromFile
    {
        private readonly ClrLanguagesKnown myClrLanguagesKnown;
        private readonly IUnitTestElementIdFactory myIdFactory;
        [NotNull] private readonly ILogger myLogger;
        private readonly UnitTestAttributeCache myUnitTestAttributeCache;
        private readonly IUnitTestElementIdFactory myUnitTestElementIdFactory;
        private readonly IUnitTestElementManager myUnitTestElementManager;
        private readonly UnityNUnitServiceProvider myServiceProvider;
        private readonly UnityTestProvider myUnityTestProvider;

        public UnityTestsSourceExplorer([NotNull] ISolution solution, [NotNull] UnityTestProvider provider, ClrLanguagesKnown clrLanguagesKnown,
            [NotNull] AssemblyToAssemblyReferencesResolveManager resolveManager, IUnitTestElementIdFactory idFactory,
            [NotNull] ResolveContextManager resolveContextManager, [NotNull] ILogger logger,
            UnitTestAttributeCache unitTestAttributeCache, IUnitTestElementIdFactory unitTestElementIdFactory, IUnitTestElementManager unitTestElementManager, UnityNUnitServiceProvider serviceProvider)
            : base(solution, provider, resolveManager, resolveContextManager, logger)
        {
            myClrLanguagesKnown = clrLanguagesKnown;
            myIdFactory = idFactory;
            myLogger = logger;
            myUnitTestAttributeCache = unitTestAttributeCache;
            myUnitTestElementIdFactory = unitTestElementIdFactory;
            myUnityTestProvider = provider;
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

        public void ProcessFile(IFile psiFile, IUnitTestElementsObserver observer, Func<bool> interrupted)
        {
            if (!myClrLanguagesKnown.AllLanguages.Any(language => Equals(language, psiFile.Language)))
                return;

            // external sources case.
            var projectFile = psiFile.GetSourceFile().ToProjectFile();
            if (projectFile == null)
                return;

            var factory = new UnityTestElementFactory(myIdFactory, myUnityTestProvider, myUnitTestElementManager, myServiceProvider);
            var fileExplorer = new UnityTestFileExplorer(psiFile, factory, myUnitTestAttributeCache, observer, interrupted, projectFile.GetProject());

            psiFile.ProcessDescendants(fileExplorer);
            observer.OnCompleted();
        }
    }
}