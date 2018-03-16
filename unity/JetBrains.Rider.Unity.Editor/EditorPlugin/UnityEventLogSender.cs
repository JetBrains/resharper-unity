using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public class UnityEventLogSender
  {
    private readonly RProperty<EditorPluginModel> myModel;

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
        if (!myDelayedLogEvents.Any()) 
          return;
        
        var head = myDelayedLogEvents.First;
        while (head != null)
        {
          SendLogEvent(model, head.Value);
          head = head.Next;
        }
        myDelayedLogEvents.Clear();
      });
    }

    private readonly int myDelayedLogEventsMaxSize = 1000;
    private readonly LinkedList<RdLogEvent> myDelayedLogEvents = new LinkedList<RdLogEvent>();

    public UnityEventLogSender(RProperty<EditorPluginModel> model)
    {
      myModel = model;
    }

    private void ApplicationOnLogMessageReceived(string message, string stackTrace, LogType type)
    {
      // use Protocol to pass log entries to Rider
      MainThreadDispatcher.Instance.InvokeOrQueue(() =>
      {
        RdLogEvent evt;
        switch (type)
        {
          case LogType.Error:
          case LogType.Exception:
            evt = new RdLogEvent(RdLogEventType.Error, EditorApplication.isPlaying ? RdLogEventMode.Play : RdLogEventMode.Edit, message, stackTrace);
            break;
          case LogType.Warning:
            evt = new RdLogEvent(RdLogEventType.Warning, EditorApplication.isPlaying ? RdLogEventMode.Play : RdLogEventMode.Edit, message, stackTrace);
            break;
          default:
            evt = new RdLogEvent(RdLogEventType.Message, EditorApplication.isPlaying ? RdLogEventMode.Play : RdLogEventMode.Edit, message, stackTrace);
            break;
        }

        var model = myModel.Maybe.ValueOrDefault;
        if (model == null)
        {
          myDelayedLogEvents.AddLast(evt);
          if (myDelayedLogEvents.Count >= myDelayedLogEventsMaxSize)
            myDelayedLogEvents.RemoveFirst(); // limit max size
        }
        else
        {
          SendLogEvent(model, evt);
        }
      });
    }

    private void SendLogEvent(EditorPluginModel model, RdLogEvent logEvent)
    {
      //if (!message.StartsWith("[Rider][TRACE]")) // avoid sending because in Trace mode log about sending log event to Rider, will also appear in unity log
      model.LogModelInitialized.Value.Log.Fire(logEvent);
    }
  }
}