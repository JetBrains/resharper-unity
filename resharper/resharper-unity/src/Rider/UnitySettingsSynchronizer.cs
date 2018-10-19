using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnitySettingsSynchronizer : UnityReferencesTracker.IHandler
    {
        private readonly Lifetime myLifetime;
        private readonly UnityHost myHost;
        private readonly IContextBoundSettingsStoreLive myBoundStore;

        public UnitySettingsSynchronizer(
            Lifetime lifetime,
            ISolution solution,
            UnityHost host,
            ISettingsStore settingsStore)
        {
            myLifetime = lifetime;
            myHost = host;
            myBoundStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
        }

        public void OnReferenceAdded(IProject unityProject, Lifetime projectLifetime)
        {
        }

        public void OnSolutionLoaded(UnityProjectsCollection solution)
        {
            var entry = myBoundStore.Schema.GetScalarEntry((UnitySettings s) => s.EnableShaderLabHippieCompletion);            
            myBoundStore.GetValueProperty<bool>(myLifetime, entry, null).Change.Advise(myLifetime, pcea =>
            {
                if (pcea.HasNew)
                {
                    myHost.PerformModelAction(rd => rd.EnableShaderLabHippieCompletion.Value = pcea.New);
                }
            });
        }
    }
}