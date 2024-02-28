using System;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.LiveTemplates;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class TemplatesLocalizationSourceUnity:ITemplatesLocalizationSource
    {
        public Type GetResourceType()
        {
            return typeof(Resources.Strings);
        }
    }
}