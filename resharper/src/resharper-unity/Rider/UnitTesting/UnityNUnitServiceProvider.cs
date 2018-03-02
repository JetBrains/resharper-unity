using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Extentions;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.BindableLinq.Extensions;
using JetBrains.DataFlow;
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
        private readonly IUnitTestResultManager myUnitTestResultManager;

        public UnityNUnitServiceProvider(ISolution solution, IPsiModules psiModules, ISymbolCache symbolCache,
            IUnitTestElementIdFactory idFactory, IUnitTestElementManager elementManager, NUnitTestProvider provider,
            ISettingsStore settingsStore, ISettingsOptimization settingsOptimization, ISettingsCache settingsCache,
            UnitTestingCachingService cachingService, IDotNetCoreSdkResolver dotNetCoreSdkResolver, 
            UnityEditorProtocol unityEditorProtocol, IUnitTestResultManager unitTestResultManager)
            : base(solution, psiModules, symbolCache, idFactory, elementManager, provider, settingsStore,
                settingsOptimization, settingsCache, cachingService, dotNetCoreSdkResolver)
        {
            mySolution = solution;
            myUnityEditorProtocol = unityEditorProtocol;
            myUnitTestResultManager = unitTestResultManager;
        }

        public override IUnitTestRunStrategy GetRunStrategy(IUnitTestElement element)
        {
            if (myUnityEditorProtocol.UnityModel.Value == null)
                return base.GetRunStrategy(element);

            var currentConnectionLifetime = Lifetimes.Define(mySolution.GetLifetime());
            myUnityEditorProtocol.UnityModel.Change.Advise_NoAcknowledgement(currentConnectionLifetime.Lifetime, (args) =>
            {
                if (args.HasNew && args.New == null)
                    currentConnectionLifetime.Terminate();
            });
            
            return new RunViaUnityEditorStrategy(mySolution, myUnityEditorProtocol.UnityModel.Value, 
                currentConnectionLifetime.Lifetime, myUnitTestResultManager);
        }
    }
}