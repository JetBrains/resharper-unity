using System;
using System.Collections.Generic;
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
    public readonly LinkedList<RdLogEvent> myDelayedLogEvents = new LinkedList<RdLogEvent>();
    private readonly object myLock = new object();

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

      RdLogEventMode eventMode = RdLogEventMode.Edit;
      MainThreadDispatcher.Instance.Queue(() =>
      {
        eventMode = EditorApplication.isPlaying ? RdLogEventMode.Play : RdLogEventMode.Edit;  
      });
      
      var evt = new RdLogEvent(DateTime.UtcNow.Ticks, eventType, eventMode, message, stackTrace);
      lock (myLock)
      {
        myDelayedLogEvents.AddLast(evt);
        if (myDelayedLogEvents.Count >= myDelayedLogEventsMaxSize)
          myDelayedLogEvents.RemoveFirst(); // limit max size
      }

      OnAddEvent(new EventArgs());
    }

    public event EventHandler AddEvent;

    private void OnAddEvent(EventArgs e)
    {
      var handler = AddEvent;
      handler?.Invoke(this, e);
    }
  }
  
  public class UnityEventLogSender
  {
    private readonly Lifetime myConnectionLifetime;

    public UnityEventLogSender(UnityEventCollector collector, Lifetime connectionLifetime)
    {
      myConnectionLifetime = connectionLifetime;
      ProcessQueue(PluginEntryPoint.UnityModel.Maybe.Value, collector);

      collector.AddEvent +=(col, _) =>
      {
        if (PluginEntryPoint.UnityModel.Maybe.HasValue && !myConnectionLifetime.IsTerminated)
          ProcessQueue(PluginEntryPoint.UnityModel.Maybe.Value, (UnityEventCollector)col);
      };
    }

    private void ProcessQueue(EditorPluginModel model, UnityEventCollector collector)
    {
      if (!collector.myDelayedLogEvents.Any())
        return;

      var head = collector.myDelayedLogEvents.First;
      while (head != null)
      {
        if (myConnectionLifetime.IsTerminated)
          return;
        
        SendLogEvent(model, head.Value);
        head = head.Next;
      }

      collector.myDelayedLogEvents.Clear();
    }
    
    private void SendLogEvent(EditorPluginModel model, RdLogEvent logEvent)
    {
      MainThreadDispatcher.Instance.Queue(() =>
      {
        if (!myConnectionLifetime.IsTerminated)
        {
          model.Log.Fire(logEvent);
        }
      });      
    }
  }
}