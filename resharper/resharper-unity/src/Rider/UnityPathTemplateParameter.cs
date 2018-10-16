using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public class UnityPathTemplateParameter : DotNetTemplateParameter
    {
        private readonly UnityMonoPathProvider myUnityMonoPathProvider;

        public UnityPathTemplateParameter(UnityMonoPathProvider unityMonoPathProvider) : base("PathToUnityEngine", "Path to UnityEngine.dll")
        {
            myUnityMonoPathProvider = unityMonoPathProvider;
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
            var possiblePath = myUnityMonoPathProvider.GetPossibleMonoPaths().Select(a=>a.Directory.Combine("Managed/UnityEngine.dll")).FirstOrDefault(b => b.ExistsFile);
            if (possiblePath != null)
                defaultPath = possiblePath.FullPath;
            return new RdProjectTemplateTextParameter(Name, defaultPath, Tooltip, RdTextParameterStyle.FileChooser, content);
        }
    }

    [ShellComponent]
    public class UnityPathParameterProvider : IDotNetTemplateParameterProvider
    {
        private readonly UnityMonoPathProvider myUnityMonoPathProvider;

        public UnityPathParameterProvider(UnityMonoPathProvider unityMonoPathProvider)
        {
            myUnityMonoPathProvider = unityMonoPathProvider;
        }
        public int Priority
        {
            get { return 50; }
        }
    
        public IReadOnlyCollection<DotNetTemplateParameter> Get()
        {
            return new[] {new UnityPathTemplateParameter(myUnityMonoPathProvider)};
        }
    }
}