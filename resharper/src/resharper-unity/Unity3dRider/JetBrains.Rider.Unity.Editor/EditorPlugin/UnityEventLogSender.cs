using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public class UnityEventLogSender
  {
    private readonly RProperty<UnityModel> myModel;

    public void UnityLogRegisterCallBack()
    {
      var eventInfo = typeof(Application).GetEvent("logMessageReceived", BindingFlags.Static | BindingFlags.Public);
      var domainLifetime = Lifetimes.Define();
      
      if (eventInfo != null)
      {
        eventInfo.AddEventHandler(null, new Application.LogCallback(ApplicationOnLogMessageReceived));
        AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
        {
          eventInfo.RemoveEventHandler(null, new Application.LogCallback(ApplicationOnLogMessageReceived));
          domainLifetime.Terminate();
        });
      }
      else
      {
#pragma warning disable 612, 618
        Application.RegisterLogCallback(ApplicationOnLogMessageReceived);
#pragma warning restore 612, 618
      }
      
      myModel.AdviseNotNull(domainLifetime.Lifetime, model =>
      {
        myDelayedLogEvents.ForEach(evt => SendLogEvent(model, evt));
        myDelayedLogEvents.Clear();
      });
    }

    private readonly List<RdLogEvent> myDelayedLogEvents = new List<RdLogEvent>();

    public UnityEventLogSender(RProperty<UnityModel> model)
    {
      myModel = model;
    }

    private void ApplicationOnLogMessageReceived(string message, string stackTrace, LogType type)
    {
      if (PluginSettings.SendConsoleToRider)
      {
        // use Protocol to pass log entries to Rider
        MainThreadDispatcher.Instance.InvokeOrQueue(() =>
        {
          RdLogEvent evt;
          switch (type)
          {
            case LogType.Error:
            case LogType.Exception:
              evt = new RdLogEvent(RdLogEventType.Error, EditorApplication.isPlaying?RdLogEventMode.Play:RdLogEventMode.Edit, message, stackTrace);
              break;
            case LogType.Warning:
              evt = new RdLogEvent(RdLogEventType.Warning, EditorApplication.isPlaying?RdLogEventMode.Play:RdLogEventMode.Edit, message, stackTrace);
              break;
            default:
              evt = new RdLogEvent(RdLogEventType.Message, EditorApplication.isPlaying?RdLogEventMode.Play:RdLogEventMode.Edit, message, stackTrace);
              break;
          }
          
          var model = myModel.Maybe.ValueOrDefault;
          if (model == null)
          {
            myDelayedLogEvents.Add(evt);
          }
          else
          {
            SendLogEvent(model, evt);
          }
        });
      }
    }
    
    private void SendLogEvent(UnityModel model, RdLogEvent logEvent)
    {
      //if (!message.StartsWith("[Rider][TRACE]")) // avoid sending because in Trace mode log about sending log event to Rider, will also appear in unity log
      model.LogModelInitialized.Value.Log.Fire(logEvent);
    }
  }
}