using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using JetBrains.DataFlow;
using JetBrains.Platform.Unity.EditorPluginModel;
using UnityEditor;
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

      var ticks = DateTime.UtcNow.Ticks;
      MainThreadDispatcher.Instance.Queue(() =>
      {
        var eventMode = EditorApplication.isPlaying ? RdLogEventMode.Play : RdLogEventMode.Edit;

        var evt = new RdLogEvent(ticks, eventType, eventMode, message, stackTrace);
        DelayedLogEvents.AddLast(evt);
        if (DelayedLogEvents.Count >= myDelayedLogEventsMaxSize)
          DelayedLogEvents.RemoveFirst(); // limit max size

        OnAddEvent(new EventArgs());
      });
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
      ProcessQueue(PluginEntryPoint.UnityModels.Where(a=>!a.Lifetime.IsTerminated).ToArray(), collector);
      collector.DelayedLogEvents.Clear();

      collector.ClearEvent();
      collector.AddEvent += (col, _) =>
      {
        var modelWithLifetimeArray = PluginEntryPoint.UnityModels.Where(a=>!a.Lifetime.IsTerminated).ToArray();
        ProcessQueue(modelWithLifetimeArray, (UnityEventCollector)col);
      };
    }

    private void ProcessQueue(ModelWithLifetime[] modelWithLifetimeArray, UnityEventCollector collector)
    {
      if (!collector.DelayedLogEvents.Any())
        return;

      var head = collector.DelayedLogEvents.First;
      while (head != null)
      {
        SendLogEvent(modelWithLifetimeArray, head.Value);
        head = head.Next;
      }
      collector.DelayedLogEvents.Clear();
    }
    
    private void SendLogEvent(ModelWithLifetime[] modelWithLifetimeArray, RdLogEvent logEvent)
    {
      MainThreadDispatcher.Instance.Queue(() =>
      {
        foreach (var modelWithLifetime in modelWithLifetimeArray)
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