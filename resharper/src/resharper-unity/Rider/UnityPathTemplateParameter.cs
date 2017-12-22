using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.Interop;

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
            switch (PlatformUtil.RuntimePlatform)
            {
                case PlatformUtil.Platform.MacOsX:
                    defaultPath = @"/Applications/Unity.app/Contents/Frameworks/Managed/UnityEngine.dll";
                    break;
                case PlatformUtil.Platform.Linux:
                    defaultPath = @"/opt/Unity/Editor/Data/Managed/UnityEngine.dll";
                    if (File.Exists(defaultPath))
                        break;
                    
                    var home = Environment.GetEnvironmentVariable("HOME");
                    if (string.IsNullOrEmpty(home))
                        break;
                        
                    var path = new DirectoryInfo(home).GetDirectories("Unity*").Select(unityDir=>Path.Combine(unityDir.FullName, @"Editor/Data/Managed/UnityEngine.dll")).FirstOrDefault(File.Exists);
                    if (path == null)
                        break;
                    defaultPath = path;
                    break;
                default:
                    defaultPath = @"C:\Program Files\Unity\Editor\Data\Managed\UnityEngine.dll";
                    if (File.Exists(defaultPath))
                        break;
                    
                    var lnks = FileSystemPath.Parse(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs").GetChildDirectories("Unity*").SelectMany(a=>a.GetChildFiles("Unity.lnk")).ToArray();
                    var dllPath = lnks.Select(a => ShellLinkHelper.ResolveLinkTarget(a).Directory.Combine(@"Data\Managed\UnityEngine.dll")).Where(b=>b.ExistsFile).OrderBy(c=>new FileInfo(c.FullPath).CreationTime).LastOrDefault();
                    if (dllPath != null)
                        defaultPath = dllPath.FullPath;
                    
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