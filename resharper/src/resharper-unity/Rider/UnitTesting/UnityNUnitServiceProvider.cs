using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Extentions;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.DotNetCore;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.ReSharper.UnitTestProvider.nUnit;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityNUnitServiceProvider : NUnitServiceProvider
    {
        private readonly ISolution mySolution;
        private readonly UnityEditorProtocol myUnityEditorProtocol;

        public UnityNUnitServiceProvider(ISolution solution, IPsiModules psiModules, ISymbolCache symbolCache,
            IUnitTestElementIdFactory idFactory, IUnitTestElementManager elementManager, NUnitTestProvider provider,
            ISettingsStore settingsStore, ISettingsOptimization settingsOptimization, ISettingsCache settingsCache,
            UnitTestingCachingService cachingService, IDotNetCoreSdkResolver dotNetCoreSdkResolver, 
            UnityEditorProtocol unityEditorProtocol)
            : base(solution, psiModules, symbolCache, idFactory, elementManager, provider, settingsStore,
                settingsOptimization, settingsCache, cachingService, dotNetCoreSdkResolver)
        {
            mySolution = solution;
            myUnityEditorProtocol = unityEditorProtocol;
        }

        protected override IUnitTestRunStrategy InitStrategy()
        {
            if (myUnityEditorProtocol.UnityModel == null)
                return new NUnitOutOfProcessUnitTestRunStrategy();
            
            return new RunViaUnityEditorStrategy();
        }
    }
}