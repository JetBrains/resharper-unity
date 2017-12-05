using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public class UnityPathTemplateParameter : DotNetTemplateParameter
    {
        public UnityPathTemplateParameter() : base("PathToUnityEngine", "Path to UnityEngine.dll")
        {
        }

        public override RdProjectTemplateContent CreateContent(DotNetProjectTemplateExpander expander, IDotNetTemplateContentFactory factory,
            int index, IDictionary<string, string> context)
        {
            var content = factory.CreateNextParameters(new[] {expander}, index + 1, context);
            var parameter = expander.TemplateInfo.GetParameter(Name);
            if (parameter == null)
            {
                return content;
            }

            var defaultPath = "";
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    defaultPath = @"/Applications/Unity.app/Contents/Frameworks/Managed/UnityEngine.dll";
                    break;
                case PlatformID.Unix:
                    defaultPath = @"/opt/Unity/Editor/Data/Managed/UnityEngine.dll";
                    break;
                default:
                    defaultPath = @"C:\Program Files\Unity\Editor\Data\Managed\UnityEngine.dll";
                    break;
            }

            return new RdProjectTemplateTextParameter(Name, defaultPath, Tooltip, RdTextParameterStyle.FileChooser, content);
        }
    }

    [ShellComponent]
    public class UnityPathParameterProvider : IDotNetTemplateParameterProvider
    {
        public int Priority
        {
            get { return 50; }
        }
    
        public IReadOnlyCollection<DotNetTemplateParameter> Get()
        {
            return new[] {new UnityPathTemplateParameter()};
        }
    }
}