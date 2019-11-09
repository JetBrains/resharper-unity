using JetBrains.Lifetimes;
using JetBrains.Rider.Model;

namespace JetBrains.Rider.Unity.Editor
{
  // TODO: See also UnityModelAndLifetime
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