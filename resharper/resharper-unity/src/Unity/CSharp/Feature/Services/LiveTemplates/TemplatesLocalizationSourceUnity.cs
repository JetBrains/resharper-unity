using System;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates
{
    [ShellComponent]
    public class TemplatesLocalizationSourceUnity:ITemplatesLocalizationSource
    {
        public Type GetResourceType()
        {
            return typeof(Resources.Strings);
        }
    }
}