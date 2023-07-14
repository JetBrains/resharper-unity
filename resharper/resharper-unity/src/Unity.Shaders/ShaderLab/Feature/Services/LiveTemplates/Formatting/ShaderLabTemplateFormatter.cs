#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Formatting;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates.Formatting
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabTemplateFormatter : ITemplateFormatter
    {
        public string EndMarker => "_end_";
        public bool SupportsShorteningQualifiedReferences => false;
        public void BeforeFormatting(DocumentRange documentRange, ISolution solution)
        {
        }

        public void ShortenQualifiedReferences(DocumentRange documentRange, ISolution solution)
        {
        }
    }
}