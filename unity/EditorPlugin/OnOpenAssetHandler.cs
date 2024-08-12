using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.PathLocator;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public class OnOpenAssetHandler
  {
    private readonly ILog myLogger = Log.GetLog<OnOpenAssetHandler>();
    private readonly Lifetime myLifetime;
    private readonly RiderPathProvider myRiderPathProvider;
    private readonly string mySlnFile;
    private readonly RiderFileOpener myOpener;

    internal OnOpenAssetHandler(Lifetime lifetime,
                                RiderPathProvider riderPathProvider,
                                string slnFile)
    {
      myLifetime = lifetime;
      myRiderPathProvider = riderPathProvider;
      mySlnFile = slnFile;
      myOpener = new RiderFileOpener(RiderPathProvider.RiderPathLocator.RiderLocatorEnvironment);
    }

    // DO NOT RENAME OR CHANGE SIGNATURE!
    // Created as a public API for external users. See https://github.com/JetBrains/resharper-unity/issues/475
    [PublicAPI]
    public bool OnOpenedAsset(string assetFilePath, int line, int column = 0)
    {
      var modifiedSource = EditorPrefs.GetBool(ModificationPostProcessor.ModifiedSource, false);
      myLogger.Verbose("ModifiedSource: {0} EditorApplication.isPlaying: {1} EditorPrefsWrapper.AutoRefresh: {2}",
        modifiedSource, EditorApplication.isPlaying, EditorPrefsWrapper.AutoRefresh);

      if (modifiedSource && !EditorApplication.isPlaying && EditorPrefsWrapper.AutoRefresh || !File.Exists(PluginEntryPoint.SlnFile))
      {
        UnityUtils.SyncSolution(); // added to handle opening file, which was just recently created.
        EditorPrefs.SetBool(ModificationPostProcessor.ModifiedSource, false);
      }

      var model = UnityEditorProtocol.Models.FirstOrDefault();
      if (model != null)
      {
        if (PluginEntryPoint.CheckConnectedToBackendSync(model))
        {
          myLogger.Verbose("Calling OpenFileLineCol: {0}, {1}, {2}", assetFilePath, line, column);

          if (model.RiderProcessId.HasValue())
            myOpener.AllowSetForegroundWindow(model.RiderProcessId.Value);
          else
            myOpener.AllowSetForegroundWindow();

          model.OpenFileLineCol.Start(myLifetime, new RdOpenFileArgs(assetFilePath, line, column));

          // todo: maybe fallback to OpenFile, if returns false
          return true;
        }
      }
      
      return OpenInRider(mySlnFile, assetFilePath, line, column);
    }

    public bool OpenInRider(string slnFile, string assetFilePath, int line, int column)
    {
      var defaultApp = myRiderPathProvider.ValidateAndReturnActualRider(EditorPrefsWrapper.ExternalScriptEditor);
      if (string.IsNullOrEmpty(defaultApp))
      {
        myLogger.Verbose("Could not find default rider app");
        return false;
      }

      return myOpener.OpenFile(defaultApp, slnFile, assetFilePath, line, column);
    }
  }
}
