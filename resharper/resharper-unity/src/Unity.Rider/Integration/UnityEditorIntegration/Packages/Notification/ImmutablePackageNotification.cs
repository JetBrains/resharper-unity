#nullable enable

using System;
using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.TextControls;
using JetBrains.ReSharper.Features.Inspections.Bookmarks.NumberedBookmarks;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Rider.Backend.Features.Notifications;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages.Notification
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class ImmutablePackageNotification
    {
        public ImmutablePackageNotification(Lifetime lifetime,
            UnitySolutionTracker solutionTracker,
            ISolution solution,
            NotificationPanelHost notificationPanelHost,
            UnityVersion unityVersion)
        {
            if (!solutionTracker.IsUnityProject.Value)
                return;

            var localPackageCacheFolder = UnityCachesFinder.GetLocalPackageCacheFolder(solution.SolutionDirectory);

            solution.GetComponent<ITextControlHost>().ViewHostTextControls(lifetime, (lt, id, textControl) =>
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
                    message = Strings.ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Unity_Package_Cache__Any_changes_made_will_be_lost_;
                }
                else if (UnityCachesFinder.GetPackagesCacheRoot().IsPrefixOf(projectFile.Location)) 
                {
                    message = Strings.ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Global_Unity_Package_Cache__Any_changes_made_will_be_lost_;
                }
                else 
                {
                    var builtInPackagesFolder = UnityCachesFinder.GetBuiltInPackagesFolder(unityVersion.GetActualAppPathForSolution());
                    if (!builtInPackagesFolder.IsEmpty && builtInPackagesFolder.IsPrefixOf(projectFile.Location))
                        message = Strings.ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_BuildIn_Unity_Package_Cache__Any_changes_made_will_be_lost_;
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