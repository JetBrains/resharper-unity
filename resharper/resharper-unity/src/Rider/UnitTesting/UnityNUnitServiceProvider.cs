﻿using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Extentions;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityNUnitServiceProvider : NUnitServiceProvider
    {
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly RunViaUnityEditorStrategy myUnityEditorStrategy;
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly FrontendBackendModel myFrontendBackendModel;

        public UnityNUnitServiceProvider(ISolution solution,
                                         IPsiModules psiModules,
                                         ISymbolCache symbolCache,
                                         IUnitTestElementIdFactory idFactory,
                                         IUnitTestElementManager elementManager,
                                         NUnitTestProvider provider,
                                         IUnitTestingSettings settings,
                                         ISettingsStore settingsStore,
                                         ISettingsOptimization settingsOptimization,
                                         ISettingsCache settingsCache,
                                         UnitTestingCachingService cachingService,
                                         INUnitTestParametersProvider testParametersProvider,
                                         FrontendBackendHost frontendBackendHost,
                                         BackendUnityHost backendUnityHost,
                                         RunViaUnityEditorStrategy runViaUnityEditorStrategy,
                                         UnitySolutionTracker unitySolutionTracker)
            : base(solution, psiModules, symbolCache, idFactory, elementManager, provider, settings, settingsStore,
                settingsOptimization, settingsCache, cachingService, testParametersProvider)
        {
            // Only in tests
            if (!frontendBackendHost.IsAvailable)
                return;

            myFrontendBackendModel = frontendBackendHost.Model.NotNull("frontendBackendHost.Model != null");
            myBackendUnityHost = backendUnityHost;
            myUnityEditorStrategy = runViaUnityEditorStrategy;
            myUnitySolutionTracker = unitySolutionTracker;
        }

        public override IUnitTestRunStrategy GetRunStrategy(IUnitTestElement element)
        {
            return IsUnityUnitTestStrategy() ? myUnityEditorStrategy : base.GetRunStrategy(element);
        }

        public static bool IsUnityUnitTestStrategy(UnitySolutionTracker unitySolutionTracker, FrontendBackendModel frontendBackendModel, BackendUnityHost backendUnityHost)
        {
            if (!unitySolutionTracker.IsUnityProjectFolder.HasTrueValue())
                return false;

            // first run from gutter mark should try to run in Unity by default. https://github.com/JetBrains/resharper-unity/issues/605
            return !frontendBackendModel.UnitTestPreference.HasValue() && backendUnityHost.BackendUnityModel.Value != null ||
                   (frontendBackendModel.UnitTestPreference.HasValue() && frontendBackendModel.UnitTestPreference.Value != UnitTestLaunchPreference.NUnit);
        }

        public bool IsUnityUnitTestStrategy()
        {
            return IsUnityUnitTestStrategy(myUnitySolutionTracker, myFrontendBackendModel, myBackendUnityHost);
        }
    }
}
