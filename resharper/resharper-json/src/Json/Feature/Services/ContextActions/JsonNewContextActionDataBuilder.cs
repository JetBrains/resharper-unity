using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.Services.ContextActions
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