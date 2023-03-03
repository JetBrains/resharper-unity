#nullable enable

using System;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.RdBackend.Common.Features.TextControls;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Rider.Backend.Features.Notifications;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages.Notification
{
    [SolutionComponent]
    public class ImmutablePackageNotification
    {
        public ImmutablePackageNotification(Lifetime lifetime,
            UnitySolutionTracker solutionTracker,
            ISolution solution,
            ITextControlHost textControlHost,
            NotificationPanelHost notificationPanelHost,
            UnityVersion unityVersion)
        {
            if (!solutionTracker.IsUnityGeneratedProject.Value)
                return;

            var localPackageCacheFolder = UnityCachesFinder.GetLocalPackageCacheFolder(solution.SolutionDirectory);

            textControlHost.ViewHostTextControls(lifetime, (lt, id, textControl) =>
            {
                if (textControl.Document is not RiderDocument riderDocument ||
                    !riderDocument.Location.ExtensionNoDot.Equals("csproj", StringComparison.OrdinalIgnoreCase))
                    return;
                
                string? message = null;
                
                if (solution.SolutionDirectory.Combine("Assets").IsPrefixOf(riderDocument.Location))
                    return;
                if (solution.SolutionDirectory.Combine("Packages").IsPrefixOf(riderDocument.Location))
                    return;
                
                if (localPackageCacheFolder.IsPrefixOf(riderDocument.Location))
                {
                    message = Strings.ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Unity_Package_Cache__Any_changes_made_will_be_lost_;
                }
                else if (UnityCachesFinder.GetPackagesCacheRoot().IsPrefixOf(riderDocument.Location)) 
                {
                    message = Strings.ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Global_Unity_Package_Cache__Any_changes_made_will_be_lost_;
                }
                else 
                {
                    var builtInPackagesFolder = UnityCachesFinder.GetBuiltInPackagesFolder(unityVersion.GetActualAppPathForSolution());
                    if (!builtInPackagesFolder.IsEmpty && builtInPackagesFolder.IsPrefixOf(riderDocument.Location))
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