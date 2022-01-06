using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Json.Feature.Services.ContextActions
{
    public class JsonNewContextActionDataProvider : CachedContextActionDataProviderBase<IJsonNewFile>,
        IJsonNewContextActionDataProvider
    {
        public JsonNewContextActionDataProvider(ISolution solution, ITextControl textControl, IJsonNewFile psiFile)
            : base(solution, textControl, psiFile)
        {
            ElementFactory = JsonNewElementFactory.GetInstance(PsiModule);
        }

        public JsonNewElementFactory ElementFactory { get; }
    }
}