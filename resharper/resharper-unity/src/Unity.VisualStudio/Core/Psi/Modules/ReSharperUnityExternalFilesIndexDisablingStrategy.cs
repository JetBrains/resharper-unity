using JetBrains.Application.Notifications;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Core.Psi.Modules
{
    [SolutionComponent]
    public class ReSharperUnityExternalFilesIndexDisablingStrategy : UnityExternalFilesIndexDisablingStrategy
    {
        private readonly Lifetime myLifetime;
        private readonly UserNotifications myNotifications;

        public ReSharperUnityExternalFilesIndexDisablingStrategy(Lifetime lifetime,
                                                                 SolutionCaches solutionCaches,
                                                                 IApplicationWideContextBoundSettingStore settingsStore,
                                                                 AssetIndexingSupport assetIndexingSupport,
                                                                 UserNotifications notifications)
            : base(solutionCaches, settingsStore, assetIndexingSupport)
        {
            myLifetime = lifetime;
            myNotifications = notifications;
        }

        protected override void NotifyAssetIndexingDisabled()
        {
            myNotifications.CreateNotification(myLifetime, NotificationSeverity.WARNING,
                "Disabled indexing of Unity assets",
                @"Due to the size of the project, indexing of Unity scenes, assets and prefabs has been disabled. Usages of C# code in these files will not be detected.");
        }
    }
}