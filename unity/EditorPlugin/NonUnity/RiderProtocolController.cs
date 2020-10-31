using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Diagnostics;
using JetBrains.Rd.Impl;

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
        ourLogger.Verbose("Start ControllerTask...");

        Wire = new SocketWire.Server(lifetime, mainThreadScheduler, null, "UnityServer");
        Wire.BackwardsCompatibleWireFormat = true;
        
        ourLogger.Verbose($"Created SocketWire with port = {Wire.Port}");
      }
      catch (Exception ex)
      {
        ourLogger.Error("RiderProtocolController.ctor. " + ex);
      }
    }
  }
  
//  [Serializable]
  struct ProtocolInstance
  {
    public string SolutionName;
    public int Port;

    public ProtocolInstance(string solutionName, int port)
    {
      SolutionName = solutionName;
      Port = port;
    }

    public static string ToJson(List<ProtocolInstance> connections)
    {
        //return JsonConvert.SerializeObject(connections); //turns out to be slow https://github.com/JetBrains/resharper-unity/issues/728 
      var sb = new StringBuilder("[");

      sb.Append(connections
        .Select(connection=> "{" + $"\"Port\":{connection.Port},\"SolutionName\":\"{connection.SolutionName}\",\"ProtocolGuid\":\"{ProtocolCompatibility.ProtocolGuid}\"" + "}")
        .Aggregate((a, b) => a + "," + b));

      sb.Append("]");
      return sb.ToString();
    }
  }
}