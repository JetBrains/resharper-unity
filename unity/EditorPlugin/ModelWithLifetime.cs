using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;

namespace JetBrains.Rider.Unity.Editor
{
  public class ModelWithLifetime
  {
    public readonly BackendUnityModel Model;
    public readonly Lifetime Lifetime;
    public ModelWithLifetime(BackendUnityModel model, Lifetime lifetime)
    {
      Model = model;
      Lifetime = lifetime;
    }
  }
}