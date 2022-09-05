#nullable enable

using System;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Features.Inspections.Bookmarks.NumberedBookmarks;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.Rider.Backend.Features.Notifications;
using JetBrains.Rider.Backend.Features.TextControls;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages.Notification
{
    [SolutionComponent]
    public class ImmutablePackageNotification
    {
        public ImmutablePackageNotification(Lifetime lifetime,
            UnitySolutionTracker solutionTracker,
            ISolution solution,
            RiderTextControlHost riderTextControlHost,
            NotificationPanelHost notificationPanelHost)
        {
            if (!solutionTracker.IsUnityGeneratedProject.Value)
                return;

            var localPackageCacheFolder = solution.SolutionDirectory.Combine("Library/PackageCache");

            riderTextControlHost.ViewHostTextControls(lifetime, (lt, id, textControl) =>
            {
                var projectFile = textControl.ToProjectFile(solution);
                if (projectFile == null)
                    return;

                if (!localPackageCacheFolder.IsPrefixOf(projectFile.Location))
                    return;

                notificationPanelHost.AddNotificationPanel(lt, textControl,
                    new NotificationPanel("This file is part of the Unity Package Cache. Any changes made will be lost.",
                        "ImmutableUnityPackage", Array.Empty<INotificationPanelHyperlink>()));
            });
        }
    }
}