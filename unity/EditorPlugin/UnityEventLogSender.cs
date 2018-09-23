using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.DataFlow;
using JetBrains.Platform.Unity.EditorPluginModel;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public class UnityEventCollector
  {
    private readonly int myDelayedLogEventsMaxSize = 1000;
    public readonly LinkedList<RdLogEvent> DelayedLogEvents = new LinkedList<RdLogEvent>();

    public UnityEventCollector()
    {
      var eventInfo = typeof(Application).GetEvent("logMessageReceivedThreaded", BindingFlags.Static | BindingFlags.Public); // Unity 2017.x+
      if (eventInfo == null)
        eventInfo = typeof(Application).GetEvent("logMessageReceived", BindingFlags.Static | BindingFlags.Public);
      var domainLifetime = Lifetimes.Define();
      
      if (eventInfo != null)
      {
        var handler = new Application.LogCallback(ApplicationOnLogMessageReceived);
        eventInfo.AddEventHandler(null, handler);
        AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
        {
          eventInfo.RemoveEventHandler(null, handler);
          domainLifetime.Terminate();
        });
      }
      else
      {
#pragma warning disable 612, 618
        Application.RegisterLogCallback(ApplicationOnLogMessageReceived);
#pragma warning restore 612, 618
      }
    }

    private void ApplicationOnLogMessageReceived(string message, string stackTrace, LogType type)
    {
      RdLogEventType eventType;
      switch (type)
      {
        case LogType.Error:
        case LogType.Exception:
          eventType = RdLogEventType.Error;
          break;
        case LogType.Warning:
          eventType = RdLogEventType.Warning;
          break;
        default:
          eventType = RdLogEventType.Message;
          break;
      }

      RdLogEventMode mode = RdLogEventMode.Play;
      if (PluginEntryPoint.PlayModeSavedState == PluginEntryPoint.PlayModeState.Stopped)
        mode = RdLogEventMode.Edit;

      var ticks = DateTime.UtcNow.Ticks;
      var evt = new RdLogEvent(ticks, eventType, mode, message, stackTrace);
      DelayedLogEvents.AddLast(evt);
      if (DelayedLogEvents.Count >= myDelayedLogEventsMaxSize)
        DelayedLogEvents.RemoveFirst(); // limit max size

      OnAddEvent(new EventArgs());
    }

    public event EventHandler AddEvent;

    public void ClearEvent()
    {
      AddEvent = null;
    }

    private void OnAddEvent(EventArgs e)
    {
      AddEvent?.Invoke(this, e);
    }
  }
  
  public class UnityEventLogSender
  {
    public UnityEventLogSender(UnityEventCollector collector)
    {
      ProcessQueue(collector);
      collector.DelayedLogEvents.Clear();

      collector.ClearEvent();
      collector.AddEvent += (col, _) =>
      {
        ProcessQueue((UnityEventCollector)col);
      };
    }

    private void ProcessQueue(UnityEventCollector collector)
    {
      if (!collector.DelayedLogEvents.Any())
        return;

      var head = collector.DelayedLogEvents.First;
      while (head != null)
      {
        SendLogEvent(head.Value);
        head = head.Next;
      }
      collector.DelayedLogEvents.Clear();
    }
    
    private void SendLogEvent(RdLogEvent logEvent)
    {
      MainThreadDispatcher.Instance.Queue(() =>
      {
        foreach (var modelWithLifetime in PluginEntryPoint.UnityModels)
        {
          if (!modelWithLifetime.Lifetime.IsTerminated)
          {
            modelWithLifetime.Model.Log.Fire(logEvent);
          }
        }
      });      
    }
  }
}