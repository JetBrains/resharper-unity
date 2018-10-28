using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
using JetBrains.Rider.Model;
using JetBrains.Util;

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
            
            var possiblePaths = myUnityMonoPathProvider.GetPossibleMonoPaths().Select(a=>a.Directory.Combine("Managed/UnityEngine.dll")).Where(b => b.ExistsFile).ToArray();
            if (possiblePaths.IsEmpty())
            {
                return new RdProjectTemplateInvalidParameter(Name, "Unity installation is not found", null, null, null, content);
            }
            
            var options = new List<RdProjectTemplateGroupOption>();
            foreach (var path in possiblePaths)
            {
                var optionContext = new Dictionary<string, string>(context) {{Name, path.FullPath}};
                var content1 = factory.CreateNextParameters(new[] {expander}, index + 1, optionContext);
                options.Add(new RdProjectTemplateGroupOption(path.FullPath, path.FullPath, content1));
            }

            options.Add(new RdProjectTemplateGroupOption("Custom", "Custom",
                new RdProjectTemplateTextParameter(Name, "", Tooltip, RdTextParameterStyle.FileChooser, content)));
            
            return new RdProjectTemplateGroupParameter(Name, possiblePaths.Last().FullPath, Tooltip, options);
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