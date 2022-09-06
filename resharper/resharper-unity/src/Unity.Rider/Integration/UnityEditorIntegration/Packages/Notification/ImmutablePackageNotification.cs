#nullable enable

using System;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Features.Inspections.Bookmarks.NumberedBookmarks;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
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
            NotificationPanelHost notificationPanelHost,
            UnityVersion unityVersion)
        {
            if (!solutionTracker.IsUnityGeneratedProject.Value)
                return;

            var localPackageCacheFolder = UnityCachesFinder.GetLocalPackageCacheFolder(solution.SolutionDirectory);

            riderTextControlHost.ViewHostTextControls(lifetime, (lt, id, textControl) =>
            {
                var projectFile = textControl.ToProjectFile(solution);
                if (projectFile == null)
                    return;
                string? message = null;
                
                if (solution.SolutionDirectory.Combine("Assets").IsPrefixOf(projectFile.Location))
                    return;
                if (solution.SolutionDirectory.Combine("Packages").IsPrefixOf(projectFile.Location))
                    return;
                
                if (localPackageCacheFolder.IsPrefixOf(projectFile.Location))
                {
                    message = "This file is part of the Unity Package Cache. Any changes made will be lost.";
                }
                else if (UnityCachesFinder.GetPackagesCacheRoot().IsPrefixOf(projectFile.Location)) 
                {
                    message = "This file is part of the Global Unity Package Cache. Any changes made will be lost.";
                }
                else 
                {
                    var builtInPackagesFolder = UnityCachesFinder.GetBuiltInPackagesFolder(unityVersion.GetActualAppPathForSolution());
                    if (!builtInPackagesFolder.IsEmpty && builtInPackagesFolder.IsPrefixOf(projectFile.Location))
                        message = "This file is part of the BuildIn Unity Package Cache. Any changes made will be lost.";
                }
                
                if (message is not null) 
                {
                    notificationPanelHost.AddNotificationPanel(lt, textControl,
                        new NotificationPanel(
                            message,
                            "ImmutableUnityPackage", Array.Empty<INotificationPanelHyperlink>()));
                }
            });
        }
    }
}