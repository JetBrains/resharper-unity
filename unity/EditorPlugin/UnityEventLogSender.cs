using System;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Unity.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  internal static class UnityEventLogSender
  {
    private static readonly BoundedSynchronizedQueue<LogEvent> ourDelayedLogEvents = new BoundedSynchronizedQueue<LogEvent>(1000);
    private static bool ourLogEventsCollectorEnabled;

    public static void Start(Lifetime lifetime)
    {
      ourLogEventsCollectorEnabled = PluginSettings.LogEventsCollectorEnabled;
      if (!ourLogEventsCollectorEnabled)
        return;

      lifetime.Bracket(() => EditorApplication.update += Process,
        () => EditorApplication.update -= Process);
      lifetime.Bracket(() => Application.logMessageReceivedThreaded += ApplicationOnLogMessageReceived,
        ()=> Application.logMessageReceivedThreaded -= ApplicationOnLogMessageReceived);

      PlayModeStateTracker.Current.Advise(lifetime, _ =>
      {
        // Work around an issue in Unity 2017.1+ that stops sending log messages to the handler when leaving play mode.
        // The issue will not be fixed because it might break compatibility of existing workarounds
        // https://issuetracker.unity3d.com/issues/general-unityengine-dot-application-dot-logmessagereceived-is-not-being-raised-after-exiting-play-mode
        // Note that although the issue says 2017.4+ it is actually 2017.1 and above. I haven't been able to test 5.x
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
          Application.logMessageReceivedThreaded -= ApplicationOnLogMessageReceived;
          Application.logMessageReceivedThreaded += ApplicationOnLogMessageReceived;
        }
      });
    }

    private static void Process()
    {
      // main thread
      ourLogEventsCollectorEnabled = PluginSettings.LogEventsCollectorEnabled;
      if (ourLogEventsCollectorEnabled)
        ProcessQueue();
    }

    private static void ApplicationOnLogMessageReceived(string message, string stackTrace, LogType type)
    {
      if (!ourLogEventsCollectorEnabled) // stop collecting, if setting was disabled
        return;

      LogEventType eventType;
      switch (type)
      {
        case LogType.Error:
        case LogType.Exception:
          eventType = LogEventType.Error;
          break;
        case LogType.Warning:
          eventType = LogEventType.Warning;
          break;
        default:
          eventType = LogEventType.Message;
          break;
      }

      // TODO: How can we tell if the message is from a player?
      // This mode reflects the editor state, not the fact that it's a play, which is of course in play mode
      var mode = PlayModeStateTracker.Current.Value == PlayModeState.Stopped ? LogEventMode.Edit : LogEventMode.Play;
      var ticks = DateTime.UtcNow.Ticks;
      var evt = new LogEvent(ticks, eventType, mode, message, stackTrace);
      ourDelayedLogEvents.Enqueue(evt);
    }

    private static void ProcessQueue()
    {
      if (UnityEditorProtocol.Models.Count > 0) // maybe worth checking Any( with .Lifetime.IsAlive ), but shows up more expensive in Profiler
      {
        LogEvent element;
        while ((element = ourDelayedLogEvents.Dequeue()) != null)
        {
          SendLogEvent(element);
        }
      }
    }

    private static void SendLogEvent(LogEvent logEvent)
    {
      foreach (var model in UnityEditorProtocol.Models)
        model.ConsoleLogging.OnConsoleLogEvent(logEvent);
    }
  }
}