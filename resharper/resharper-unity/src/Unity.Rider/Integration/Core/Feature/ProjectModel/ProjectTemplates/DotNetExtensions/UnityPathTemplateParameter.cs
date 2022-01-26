using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Rider.Backend.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.Rider.Backend.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.ProjectModel.ProjectTemplates.DotNetExtensions
{
    public class UnityPathTemplateParameter : DotNetTemplateParameter
    {
        public UnityPathTemplateParameter() : base("PathToUnityEngine", "Path to UnityEngine.dll", "Path to UnityEngine.dll")
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

            var possiblePaths = UnityInstallationFinder.GetPossibleMonoPaths().Select(a=>a.Directory.Combine("Managed/UnityEngine.dll")).Where(b => b.ExistsFile).ToArray();
            var options = new List<RdProjectTemplateGroupOption>();

            foreach (var path in possiblePaths)
            {
                var optionContext = new Dictionary<string, string>(context) {{Name, path.FullPath}};
                var content1 = factory.CreateNextParameters(new[] {expander}, index + 1, optionContext);
                options.Add(new RdProjectTemplateGroupOption(path.FullPath, path.FullPath, null, content1));
            }

            options.Add(new RdProjectTemplateGroupOption(
                "Custom",
                possiblePaths.Any()?"Custom":"Custom (Unity installation was not found)",
                null,
                new RdProjectTemplateTextParameter(Name, "Custom path", null, Tooltip, RdTextParameterStyle.FileChooser, content)));

            return new RdProjectTemplateGroupParameter(Name, "UnityEngineDll",
                possiblePaths.Any()?possiblePaths.Last().FullPath:string.Empty, null, options);
        }
    }

    [ShellComponent]
    public class UnityPathParameterProvider : IDotNetTemplateParameterProvider
    {
        public int Priority => 50;

        public IReadOnlyCollection<DotNetTemplateParameter> Get()
        {
            return new[] {new UnityPathTemplateParameter()};
        }
    }
}