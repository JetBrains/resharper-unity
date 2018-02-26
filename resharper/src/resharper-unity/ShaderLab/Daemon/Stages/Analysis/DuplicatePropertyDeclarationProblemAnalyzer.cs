using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IPropertiesValue), HighlightingTypes = new[] { typeof(ShaderLabFirstDuplicatePropertyWarning), typeof(ShaderLabSubsequentDuplicatePropertyWarning)})]
    public class DuplicatePropertyDeclarationProblemAnalyzer : ShaderLabElementProblemAnalyzer<IPropertiesValue>
    {
        protected override void Analyze(IPropertiesValue element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var propertiesByName = new OneToListMap<string, IPropertyDeclaration>();
            foreach (var propertyDeclaration in element.DeclarationsEnumerable)
            {
                var propertyName = propertyDeclaration.Name?.GetText();
                if (string.IsNullOrEmpty(propertyName))
                    continue;

                propertiesByName.AddValue(propertyName, propertyDeclaration);
            }

            foreach (var pair in propertiesByName)
            {
                if (pair.Value.Count > 1)
                {
                    var propertyDeclaration = pair.Value[0];
                    consumer.AddHighlighting(new ShaderLabFirstDuplicatePropertyWarning(propertyDeclaration, pair.Key,
                        propertyDeclaration.Name.GetHighlightingRange()));
                    for (var i = 1; i < pair.Value.Count; i++)
                    {
                        propertyDeclaration = pair.Value[i];
                        consumer.AddHighlighting(new ShaderLabSubsequentDuplicatePropertyWarning(propertyDeclaration,
                            pair.Key, propertyDeclaration.Name.GetHighlightingRange()));
                    }
                }
            }
        }
    }
}