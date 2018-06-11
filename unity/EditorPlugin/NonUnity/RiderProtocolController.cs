using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Newtonsoft.Json;

namespace JetBrains.Rider.Unity.Editor.NonUnity
{
  // ReSharper disable once UnusedMember.Global
  public class RiderProtocolController
  {
    public readonly SocketWire.Server Wire;
    private static readonly ILog ourLogger = Log.GetLog<RiderProtocolController>();

    public RiderProtocolController(IScheduler mainThreadScheduler, Lifetime lifetime)
    {
      try
      {
        ourLogger.Log(LoggingLevel.VERBOSE, "Start ControllerTask...");

        Wire = new SocketWire.Server(lifetime, mainThreadScheduler, null, "UnityServer", true);
        ourLogger.Log(LoggingLevel.VERBOSE, $"Created SocketWire with port = {Wire.Port}");
      }
      catch (Exception ex)
      {
        ourLogger.Error("RiderProtocolController.ctor. " + ex);
      }
    }
  }
  
  [Serializable]
  class Connection
  {
    public int Port;
    public string SolutionName;

    public Connection(int port, string solutionName)
    {
      Port = port;
      SolutionName = solutionName;
    }

    public static string ToJson(List<Connection> connections)
    {
        return JsonConvert.SerializeObject(connections);
    }
  }
}