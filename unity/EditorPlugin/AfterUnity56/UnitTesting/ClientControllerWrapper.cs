using System;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  internal class ClientControllerWrapper
  {
    private static readonly ILog ourLogger = Log.GetLog(typeof(ClientControllerWrapper));

    public static ClientControllerWrapper TryCreate(string sessionId, UnitTestLaunchClientControllerInfo clientControllerInfo)
    {
      if (clientControllerInfo == null)
      {
        ourLogger.Verbose($"ClientController not specified (SessionId={sessionId})");
        return null;
      }

      ourLogger.Verbose($"ClientController specified (SessionId={sessionId}): {clientControllerInfo.TypeName}, {clientControllerInfo.CodeBase}");

      try
      {
        if (clientControllerInfo.CodeBaseDependencies != null)
          foreach (var dependency in clientControllerInfo.CodeBaseDependencies)
          {
            ourLogger.Trace("Loading assembly from '{0}'", dependency);
            Assembly.LoadFrom(dependency);
          }

        ourLogger.Trace("Loading assembly from '{0}'", clientControllerInfo.CodeBase);
        var assembly = Assembly.LoadFrom(clientControllerInfo.CodeBase);

        var type = assembly.GetType(clientControllerInfo.TypeName);
        if (type == null)
        {
          ourLogger.Error("Type '{0}' not found in assembly '{1}'", clientControllerInfo.TypeName, assembly.FullName);
          return null;
        }
        
        ourLogger.Trace("ClientController type found: {0}", type.AssemblyQualifiedName);

        var clientController = Activator.CreateInstance(type, sessionId);

        var onSessionStartedMethodInfo = type.GetMethod("OnSessionStarted", BindingFlags.Instance | BindingFlags.Public);
        if (onSessionStartedMethodInfo == null)
        {
          ourLogger.Error("OnSessionStarted method not found in ClientController of type='{0}'", type.AssemblyQualifiedName);
          return null;
        }

        var onTestStartedMethodInfo = type.GetMethod("OnTestStarted", BindingFlags.Instance | BindingFlags.Public);
        if (onTestStartedMethodInfo == null)
        {
          ourLogger.Error("OnTestStarted method not found in ClientController of type='{0}'", type.AssemblyQualifiedName);
          return null;
        }

        var onTestFinishedMethodInfo = type.GetMethod("OnTestFinished", BindingFlags.Instance | BindingFlags.Public);
        if (onTestFinishedMethodInfo == null)
        {
          ourLogger.Error("OnTestFinished method not found in ClientController of type='{0}'", type.AssemblyQualifiedName);
          return null;
        }

        var onSessionFinishedMethodInfo = type.GetMethod("OnSessionFinished", BindingFlags.Instance | BindingFlags.Public);
        if (onSessionFinishedMethodInfo == null)
        {
          ourLogger.Error("OnSessionFinished method not found in ClientController of type='{0}'", type.AssemblyQualifiedName);
          return null;
        }

        return new ClientControllerWrapper(clientController,
                                           onSessionStartedMethodInfo,
                                           onTestStartedMethodInfo,
                                           onTestFinishedMethodInfo,
                                           onSessionFinishedMethodInfo);
      }
      catch (Exception e)
      {
        ourLogger.Error("Failed to create ClientController", e);
        return null;
      }
    }

    private readonly object myClientController;
    private readonly MethodInfo myOnSessionStartedMethodInfo;
    private readonly MethodInfo myOnTestStartedMethodInfo;
    private readonly MethodInfo myOnTestFinishedMethodInfo;
    private readonly MethodInfo myOnSessionFinishedMethodInfo;

    private ClientControllerWrapper(object clientController, MethodInfo onSessionStartedMethodInfo, MethodInfo onTestStartedMethodInfo, MethodInfo onTestFinishedMethodInfo, MethodInfo onSessionFinishedMethodInfo)
    {
      myClientController = clientController;
      myOnSessionStartedMethodInfo = onSessionStartedMethodInfo;
      myOnTestStartedMethodInfo = onTestStartedMethodInfo;
      myOnTestFinishedMethodInfo = onTestFinishedMethodInfo;
      myOnSessionFinishedMethodInfo = onSessionFinishedMethodInfo;
    }

    public void OnSessionStarted()
    {
      try
      {
        myOnSessionStartedMethodInfo.Invoke(myClientController, EmptyArray<object>.Instance);
      }
      catch (Exception e)
      {
        ourLogger.Error("Failed to invoke ClientController.OnSessionStarted method", e);
      }
    }

    public void OnTestStarted(string testId)
    {
      try
      {
        myOnTestStartedMethodInfo.Invoke(myClientController, new object[] {testId});
      }
      catch (Exception e)
      {
        ourLogger.Error("Failed to invoke ClientController.OnTestStarted method", e);
      }
    }

    public void OnTestFinished()
    {
      try
      {
        myOnTestFinishedMethodInfo.Invoke(myClientController, EmptyArray<object>.Instance);
      }
      catch (Exception e)
      {
        ourLogger.Error("Failed to invoke ClientController.OnTestFinished method", e);
      }
    }

    public void OnSessionFinished()
    {
      try
      {
        myOnSessionFinishedMethodInfo.Invoke(myClientController, EmptyArray<object>.Instance);
      }
      catch (Exception e)
      {
        ourLogger.Error("Failed to invoke ClientController.OnSessionFinished method", e);
      }
    }
  }
}