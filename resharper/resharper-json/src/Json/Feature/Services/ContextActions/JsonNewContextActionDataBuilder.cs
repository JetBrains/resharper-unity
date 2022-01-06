using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Json.Feature.Services.ContextActions
{
    [ContextActionDataBuilder(typeof(IJsonNewContextActionDataProvider))]
    public class JsonNewContextActionDataBuilder : ContextActionDataBuilderBase<JsonNewLanguage, IJsonNewFile>
    {
        public override IContextActionDataProvider BuildFromPsi(ISolution solution, ITextControl textControl,
                                                                IJsonNewFile psiFile)
        {
            return new JsonNewContextActionDataProvider(solution, textControl, psiFile);
        }
    }
}