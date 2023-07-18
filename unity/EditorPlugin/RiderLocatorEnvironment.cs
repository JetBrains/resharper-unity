using System;
using JetBrains.Diagnostics;
using JetBrains.Rider.PathLocator;

namespace JetBrains.Rider.Unity.Editor
{
  class RiderLocatorEnvironment : IRiderLocatorEnvironment
  {
    public OS CurrentOS => PluginSettings.SystemInfoRiderPlugin.OS;

    public T FromJson<T>(string json)
    {
#if UNITY_4_7
      return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
#else
      return (T)UnityEngine.JsonUtility.FromJson(json, typeof(T));
#endif 
    }
    
    public void Info(string message, Exception e = null)
    {
      var logger = Log.GetLog(GetType());
      logger.Info(message, e);
    }

    public void Warn(string message, Exception e = null)
    {
      var logger = Log.GetLog(GetType());
      logger.Warn(message, e);
    }

    public void Error(string message, Exception e)
    {
      var logger = Log.GetLog(GetType());
      logger.Error(message, e);
    }

    public void Verbose(string message, Exception e = null)
    {
      var logger = Log.GetLog(GetType());
      logger.Verbose(message, e);
    }
  }
}