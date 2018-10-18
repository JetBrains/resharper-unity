using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Extentions;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.DotNetCore;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.ReSharper.UnitTestProvider.nUnit;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityNUnitServiceProvider : NUnitServiceProvider
    {
        private readonly RunViaUnityEditorStrategy myUnityEditorStrategy;
        private readonly RdUnityModel myRdUnityModel;
        private readonly IProperty<EditorPluginModel> myUnityEditorModel;

        public UnityNUnitServiceProvider(ISolution solution, IPsiModules psiModules, ISymbolCache symbolCache,
            IUnitTestElementIdFactory idFactory, IUnitTestElementManager elementManager, NUnitTestProvider provider,
            ISettingsStore settingsStore, ISettingsOptimization settingsOptimization, ISettingsCache settingsCache,
            UnitTestingCachingService cachingService, IDotNetCoreSdkResolver dotNetCoreSdkResolver,
            UnityEditorProtocol unityEditorProtocol,
            RunViaUnityEditorStrategy runViaUnityEditorStrategy,
            NUnitOutOfProcessUnitTestRunStrategy nUnitOutOfProcessUnitTestRunStrategy)
            : base(solution, psiModules, symbolCache, idFactory, elementManager, provider, settingsStore,
                settingsOptimization, settingsCache, cachingService, dotNetCoreSdkResolver, nUnitOutOfProcessUnitTestRunStrategy)
        {
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
            
            myRdUnityModel = solution.GetProtocolSolution().GetRdUnityModel();

            myUnityEditorModel = unityEditorProtocol.UnityModel;
            myUnityEditorStrategy = runViaUnityEditorStrategy;
        }

        public override IUnitTestRunStrategy GetRunStrategy(IUnitTestElement element)
        {
            if (myUnityEditorModel.Value == null)
                return base.GetRunStrategy(element);

            // first run from gutter mark should try to run in Unity by default. https://github.com/JetBrains/resharper-unity/issues/605
            if (!myRdUnityModel.UnitTestPreference.HasValue() ||
                (myRdUnityModel.UnitTestPreference.HasValue() && myRdUnityModel.UnitTestPreference.Value != UnitTestLaunchPreference.NUnit))
                return myUnityEditorStrategy;
            
            return base.GetRunStrategy(element);

        }
    }
}
