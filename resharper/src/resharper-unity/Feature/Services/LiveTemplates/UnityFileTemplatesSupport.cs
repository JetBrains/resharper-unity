using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Support;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates
{
    // Used when creating templates from legacy XML.
    // Not really sure if we need it, to be honest
    [FileTemplates]
    public class UnityFileTemplatesSupport : IFileTemplatesSupport
    {
        public string Name => "Unity";

        public IEnumerable<ITemplateScopePoint> ScopePoints
        {
            get
            {
                yield return new InUnityCSharpProject();
                yield return new InUnityCSharpAssetsFolder();

                yield return new InUnityCSharpEditorFolder();
                yield return new InUnityCSharpRuntimeFolder();

                yield return new InUnityCSharpFirstpassFolder();
                yield return new InUnityCSharpFirstpassEditorFolder();
                yield return new InUnityCSharpFirstpassRuntimeFolder();
            }
        }
    }
}