using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Lifetimes;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.RunMarkers;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.RichText;
using JetBrains.UI.ThemedIcons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Rider.Host.Features.RunMarkers
{
  public class UnityStaticMethodRunMarkerGutterMark : RunMarkerGutterMark
  {
    public UnityStaticMethodRunMarkerGutterMark() : base(RunMarkersThemedIcons.RunActions.Id)
    {
    }

    public override IEnumerable<BulbMenuItem> GetBulbMenuItems(IHighlighter highlighter)
    {
      if (!(highlighter.UserData is UnityRunMarkerHighlighting runMarker)) yield break;

      var solution = Shell.Instance.GetComponent<SolutionsManager>().Solution;
      if (solution == null) yield break;

      switch (runMarker.AttributeId)
      {
        case UnityRunMarkerAttributeIds.RUN_METHOD_MARKER_ID:
          foreach (var item in GetRunMethodItems(solution, runMarker))
          {
            yield return item;
          }
          yield break;
        
        default:
          yield break;
      }
    }

    private IEnumerable<BulbMenuItem> GetRunMethodItems(ISolution solution, UnityRunMarkerHighlighting runMarker)
    {
      var logger = solution.GetComponent<ILogger>();
      var editorProtocol = solution.GetComponent<UnityEditorProtocol>();
      var methodFqn = DeclaredElementPresenter.Format(runMarker.Method.PresentationLanguage,
        DeclaredElementPresenter.QUALIFIED_NAME_PRESENTER, runMarker.Method).Text;

      var iconId = RunMarkersThemedIcons.RunThis.Id;
      yield return new BulbMenuItem(
        new ExecutableItem(() => { 
          
          var model = editorProtocol.UnityModel.Value;
          if (model == null) return;
          var data = new RunMethodData(
            runMarker.Project.GetOutputFilePath(runMarker.TargetFrameworkId).NameWithoutExtension,
            runMarker.Method.GetContainingType().GetClrName().FullName,
            runMarker.Method.ShortName);
          Lifetime.Using(lifetime =>
          {
            var rdTask = model.RunMethodInUnity.Start(data);
            rdTask?.Result.Advise(lifetime, result =>
            {
              if (!result.Result.Success)
                logger.Warn($"Executing {methodFqn} failed {result.Error.ReasonMessage} {result.Error.ReasonText} {result.Error.StackTrace}.");
            });
          });
        }),
        new RichText($"Run '{methodFqn}'"),
        iconId,
        BulbMenuAnchors.PermanentBackgroundItems);
    }
  }
}