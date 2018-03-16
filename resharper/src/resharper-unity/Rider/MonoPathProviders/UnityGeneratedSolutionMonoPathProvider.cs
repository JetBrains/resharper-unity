using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.Runtime;
using JetBrains.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.MonoPathProviders
{
    [SolutionComponent]
    public class UnityGeneratedSolutionMonoPathProvider : IMonoPathProvider
    {
        private readonly ISolution mySolution;

        public UnityGeneratedSolutionMonoPathProvider(ISolution solution)
        {
            mySolution = solution;
        }

        public List<FileSystemPath> GetPossibleMonoPaths()
        {
            var solFolder = mySolution.SolutionFilePath.Directory;
            var editorInstancePath = solFolder.Combine("Library/EditorInstance.json");

            var jobject = JsonConvert.DeserializeObject<JObject>(editorInstancePath.ReadAllText2().Text);
            var appContentsPath = jobject.GetValue("app_contents_path");
            
            if (appContentsPath == null) 
                return new List<FileSystemPath>();
            
            var path = FileSystemPath.Parse(appContentsPath.ToString()).Combine("MonoBleedingEdge");
            if (path.ExistsDirectory)
                return new List<FileSystemPath>(new[] {path});

            return new List<FileSystemPath>();
        }

        public int GetPriority()
        {
            return 100;
        }
    }
}