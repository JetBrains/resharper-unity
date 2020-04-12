using JetBrains.Application.Notifications;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.VisualStudio.Psi.Modules
{
    [SolutionComponent]
    public class ReSharperUnityYamlDisableStrategy : UnityYamlDisableStrategy
    {
        private readonly Lifetime myLifetime;
        private readonly UserNotifications myNotifications;

        public ReSharperUnityYamlDisableStrategy(Lifetime lifetime, ISolution solution, SolutionCaches solutionCaches, ISettingsStore settingsStore,
                                                 AssetIndexingSupport assetIndexingSupport, UserNotifications notifications)
            : base(lifetime, solution, solutionCaches, settingsStore, assetIndexingSupport)
        {
            myLifetime = lifetime;
            myNotifications = notifications;
        }

        protected override void NotifyYamlParsingDisabled()
        {
            myNotifications.CreateNotification(myLifetime, NotificationSeverity.WARNING,
                "Disabled parsing of Unity assets",
                @"Due to the size of the project, parsing of Unity scenes, assets and prefabs has been disabled. Usages of C# code in these files will not be detected.");
        }
    }
}