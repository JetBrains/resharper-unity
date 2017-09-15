using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Application.BuildScript.Application;
using JetBrains.Application.Logging;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.Unity.Model;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static class RiderController
  {
    static RiderController()
    {
      Start(Path.Combine(Path.GetTempPath(), "Unity3dRider", "Unity3dRider" + DateTime.Now.ToString("YYYY-MM-ddT-HH-mm-ss") + ".log"));
    }

    private static void Start(string loggerPath)
    {     
      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
      var lifetime = lifetimeDefinition.Lifetime;
      
      ILogger myLogger = Logger.GetLogger("Core");
      var fileLogEventListener = new FileLogEventListener(loggerPath, false); // works in Unity mono 4.6
      //var fileLogEventListener = new FileLogEventListener(loggerPath); //fails in Unity mono 4.6
      LogManager.Instance.AddOmnipresentLogger(lifetime, fileLogEventListener, LoggingLevel.VERBOSE);

      try
      {
        myLogger.Info("Unity process with Id = " + Process.GetCurrentProcess().Id);
        myLogger.Info("Start ControllerTask...");

        int port = 46000 + Process.GetCurrentProcess().Id % 1000;

        var dispatcher = new RdSimpleDispatcher(lifetime, myLogger);

        myLogger.Info("Create protocol...");
        var protocol = new Protocol(new Serializers(), new Identities(IdKind.DynamicClient), dispatcher,
          creatingProtocol =>
          {
            myLogger.Info("Creating SocketWire with port = {0}", port);
            return new SocketWire.Server(lifetime, creatingProtocol, port, "UnityServer");
          });

        myLogger.Info("Create UnityModel and advise for new sessions...");
        var model = new UnityModel(lifetime, protocol);
        model.Play.Advise(lifetime, session =>
        {
          myLogger.Info("model.Play.Advise: " + session);
          if (!session) return;
          var text = "Edit/Play";
          Dispatcher.Dispatch(() =>
          {
            EditorApplication.ExecuteMenuItem(text);
          });
        });
        model.HostConnected.SetValue(true);

        //myLogger.Info("Run dispatcher...");
        //dispatcher.Run(); // Unity already has dispatcher
      }
      catch (Exception ex)
      {
        myLogger.Error(ex);
      }
    }
  }
}