using JetBrains.DataFlow;
using JetBrains.Platform.Unity.EditorPluginModel;

namespace JetBrains.Rider.Unity.Editor
{
  public class ModelWithLifetime
  {
    public EditorPluginModel Model;
    public Lifetime Lifetime;
    public ModelWithLifetime(EditorPluginModel model, Lifetime lifetime)
    {
      Model = model;
      Lifetime = lifetime;
    }
  }
}