using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Feature.Services.ContextActions
{
    public interface IJsonNewContextActionDataProvider : IContextActionDataProvider<IJsonNewFile>
    {
        [NotNull] JsonNewElementFactory ElementFactory { get; }
    }
}