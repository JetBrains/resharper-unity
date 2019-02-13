using JetBrains.Lifetimes;
using JetBrains.Platform.Unity.EditorPluginModel;

namespace JetBrains.Rider.Unity.Editor
{
  public class ModelWithLifetime
  {
    public readonly EditorPluginModel Model;
    public readonly Lifetime Lifetime;
    public ModelWithLifetime(EditorPluginModel model, Lifetime lifetime)
    {
      Model = model;
      Lifetime = lifetime;
    }
  }
}