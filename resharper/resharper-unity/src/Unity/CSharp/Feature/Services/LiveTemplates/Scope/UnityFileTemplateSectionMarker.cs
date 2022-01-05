using System;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    // A dummy scope point that all Unity templates declare so that they are sorted and grouped together in Rider.
    // Rider uses the first scope point to group templates into sections (see RIDER-10132). If templates are available
    // due to different scope points (e.g. InUnityCSharpProject vs InUnityCSharpEditorFolder), then the templates are
    // shown in separate sections, even if they are both conceptually "Unity" file templates. This also messes up UITag
    // grouping, as each section will have it's own UITag instance, and we get multiple "Unity Class" entries.
    // Introducing a dummy scope point as the first scope point for all Unity file templates means Rider will group all
    // Unity file templates into a single section, and everything works as expected. This scope point is never published
    // by a provider, so it doesn't matter if all templates declare it. It is visible in the UI for the templates, but
    // appears as "Unity file template", so is not a major issue.
    // Sort order of section groups is undefined (or at least implicit based on settings storage). The front end will
    // boost templates in the C#/F#/VB project groups, and also Razor/Blazor templates.
    // Bizarrely, presentable name does seem to matter, though. "Unity file template" sorts above resources and
    // config files, while "Unity file template group" sorts below. I see no reason for this, so I guess it's just a
    // side effect of how things are stored/read from settings storage.
    public class UnityFileTemplateSectionMarker : TemplateScopePoint
    {
        private static readonly Guid ourDefaultUID = new Guid("C57D3F9D-BF90-42FB-BA95-2AEFA86AA872");

        public override Guid GetDefaultUID() => ourDefaultUID;
        public override string PresentableShortName => "Unity file template";
    }
}