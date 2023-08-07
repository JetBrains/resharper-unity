using System;

namespace JetBrains.Rider.PathLocator
{
  public interface IRiderLocatorEnvironment
  {
    OS CurrentOS { get; }

    T FromJson<T>(string json);

    void Info(string message, Exception e = null);
    void Warn(string message, Exception e = null);
    void Error(string message, Exception e = null);
    void Verbose(string message, Exception e = null);
  }
}