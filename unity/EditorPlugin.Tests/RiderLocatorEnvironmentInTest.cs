using System;
using JetBrains.Rider.PathLocator;
using Newtonsoft.Json;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  
  public class RiderLocatorEnvironmentInTest : IRiderLocatorEnvironment
  {
    public RiderLocatorEnvironmentInTest()
    {
    }
    
    public OS CurrentOS => OS.Windows;

    public T FromJson<T>(string json)
    {
      return JsonConvert.DeserializeObject<T>(json);
    }

    public void Info(string message, Exception e = null)
    {
      // if (e == null)
      //   myOutput.WriteLine(message);
      // else
      //   myOutput.WriteLine(e.Message);
    }

    public void Warn(string message, Exception e = null)
    {
      Info(message, e);
    }

    public void Error(string message, Exception e = null)
    {
      Info(message, e);
    }
  }
}