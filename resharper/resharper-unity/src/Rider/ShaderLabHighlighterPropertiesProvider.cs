using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.Daemon;
using JetBrains.ReSharper.Host.Features.Daemon.Registration;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages;
using JetBrains.Rider.Model.HighlighterRegistration;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [ShellComponent]
    public class ShaderLabHighlighterPropertiesProvider : IRiderHighlighterPropertiesProvider
    {
        public bool Applicable(RiderHighlighterDescription description)
        {
            return description.AttributeId == ShaderLabHighlightingAttributeIds.INJECTED_LANGUAGE_FRAGMENT;
        }

        public HighlighterProperties GetProperties(RiderHighlighterDescription description)
        {
            return new HighlighterProperties(
                description.Kind.ToModel(), !description.NotRecyclable,
                GreedySide.NONE, false, false, false);
        }

        public int Priority => 0;
    }
}