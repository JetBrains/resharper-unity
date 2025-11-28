using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Backend.Features.RunMarkers;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Rider.Model.Unity;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.RichText;
using JetBrains.UI.ThemedIcons;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.RunMarkers
{
    public class UnityStaticMethodRunMarkerGutterMark : RunMarkerGutterMark<UnityRunMarkerHighlighting>
    {
        public UnityStaticMethodRunMarkerGutterMark()
            : base(RunMarkersThemedIcons.RunActions.Id)
        {
        }

        protected override IEnumerable<BulbMenuItem> GetBulbMenuItems(ISolution solution,
            UnityRunMarkerHighlighting runMarker,
            IHighlighter highlighter)
        {
            var backendUnityHost = solution.GetComponent<BackendUnityHost>();
            var notificationsModel = solution.GetComponent<NotificationsModel>();

            var methodFqn = DeclaredElementPresenter.Format(runMarker.Method.PresentationLanguage,
                DeclaredElementPresenter.QUALIFIED_NAME_PRESENTER, runMarker.Method).Text;

            var iconId = RunMarkersThemedIcons.RunThis.Id;
            yield return new BulbMenuItem(new ExecutableItem(() =>
                {
                    var model = backendUnityHost.BackendUnityModel.Value;
                    if (model == null)
                    {
                        var notification = new NotificationModel(solution.GetRdProjectId(),
                            Strings.UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_No_connection_to_Unity,
                            Strings.UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_Make_sure_Unity_is_running_,
                            true, RdNotificationEntryType.WARN, new List<NotificationHyperlink>());
                        notificationsModel.Notification(notification);
                        return;
                    }

                    var containingType =
                        runMarker.Method.ContainingType.NotNull("runMarker.Method.ContainingType != null");
                    var data = new RunMethodData(
                        runMarker.Project.GetOutputFilePath(runMarker.TargetFrameworkId).NameWithoutExtension,
                        containingType.GetClrName().FullName, runMarker.Method.ShortName);
                    model.RunMethodInUnity.Start(solution.GetSolutionLifetimes().UntilSolutionCloseLifetime, data);
                }),
                new RichText($"Run '{methodFqn}'"),
                iconId,
                BulbMenuAnchors.PermanentBackgroundItems);
        }
    }
}