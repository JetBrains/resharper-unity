using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Extentions;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityNUnitServiceProvider : NUnitServiceProvider
    {
        private readonly UnityEditorProtocol myEditorProtocol;
        private readonly RunViaUnityEditorStrategy myUnityEditorStrategy;
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly RdUnityModel myRdUnityModel;

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
            UnityEditorProtocol editorProtocol,
            RunViaUnityEditorStrategy runViaUnityEditorStrategy,
            UnitySolutionTracker unitySolutionTracker)
            : base(solution, psiModules, symbolCache, idFactory, elementManager, provider, settings, settingsStore,
                settingsOptimization, settingsCache, cachingService, testParametersProvider)
        {
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
            
            myRdUnityModel = solution.GetProtocolSolution().GetRdUnityModel();
            myEditorProtocol = editorProtocol;
            myUnityEditorStrategy = runViaUnityEditorStrategy;
            myUnitySolutionTracker = unitySolutionTracker;
        }

        public override IUnitTestRunStrategy GetRunStrategy(IUnitTestElement element)
        {
            return IsUnityUnitTestStrategy(myUnitySolutionTracker, myRdUnityModel, myEditorProtocol) ? myUnityEditorStrategy : base.GetRunStrategy(element);
        }

        public static bool IsUnityUnitTestStrategy(UnitySolutionTracker unitySolutionTracker, RdUnityModel rdUnityModel, UnityEditorProtocol editorProtocol)
        {
            if (!unitySolutionTracker.IsUnityProjectFolder.HasTrueValue())
                return false;

            // first run from gutter mark should try to run in Unity by default. https://github.com/JetBrains/resharper-unity/issues/605
            return !rdUnityModel.UnitTestPreference.HasValue() && editorProtocol.UnityModel.Value != null ||
                   (rdUnityModel.UnitTestPreference.HasValue() && rdUnityModel.UnitTestPreference.Value != UnitTestLaunchPreference.NUnit);
        }
    }
}
