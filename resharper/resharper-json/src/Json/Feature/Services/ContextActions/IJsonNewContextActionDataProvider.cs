using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.Services.ContextActions
{
    public interface IJsonNewContextActionDataProvider : IContextActionDataProvider<IJsonNewFile>
    {
        [NotNull] JsonNewElementFactory ElementFactory { get; }
    }
}