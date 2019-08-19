using System;
using System.Reflection;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rider.Unity.Editor.NonUnity;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public class UnityEventCollector
  {
    public readonly BoundedSynchronizedQueue<RdLogEvent> DelayedLogEvents = new BoundedSynchronizedQueue<RdLogEvent>(1000);

    public UnityEventCollector()
    {
      if (!PluginSettings.LogEventsCollectorEnabled)
        return;
      
      var eventInfo = typeof(Application).GetEvent("logMessageReceivedThreaded", BindingFlags.Static | BindingFlags.Public); // Unity 2017.x+
      if (eventInfo == null)
        eventInfo = typeof(Application).GetEvent("logMessageReceived", BindingFlags.Static | BindingFlags.Public);

      if (eventInfo != null)
      {
        var handler = new Application.LogCallback(ApplicationOnLogMessageReceived);
        eventInfo.AddEventHandler(null, handler);
        AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
        {
          eventInfo.RemoveEventHandler(null, handler);
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
      DelayedLogEvents.Enqueue(evt);
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

      collector.ClearEvent();
      collector.AddEvent += (col, _) =>
      {
        ProcessQueue((UnityEventCollector)col);
      };
    }
    
    private void ProcessQueue(UnityEventCollector collector)
    {
      RdLogEvent element;
      while ((element  = collector.DelayedLogEvents.Dequeue()) != null)
      {
        SendLogEvent(element);
      }
    }
    
    private void SendLogEvent(RdLogEvent logEvent)
    {
      MainThreadDispatcher.Instance.Queue(() =>
      {
        foreach (var modelWithLifetime in PluginEntryPoint.UnityModels)
        {
          if (modelWithLifetime.Lifetime.IsAlive)
          {
            modelWithLifetime.Model.Log(logEvent);
          }
        }
      });  
    }
  }
}