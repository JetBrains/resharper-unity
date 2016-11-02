using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches
{
    [SolutionComponent]
    public class LangVersionCacheProvider : IProjectFileDataProvider<LangVersionCache>
    {
        private static readonly LangVersionCache ImplicitLangVersion = new LangVersionCache(false);
        private static readonly LangVersionCache ExplicitLangVersion = new LangVersionCache(true);

        private readonly ISolution mySolution;
        private readonly IProjectFileDataCache myCache;
        private readonly Dictionary<FileSystemPath, Action> myCallbacks;

        public LangVersionCacheProvider(Lifetime lifetime, ISolution solution, IProjectFileDataCache cache)
        {
            mySolution = solution;
            myCache = cache;

            myCache.RegisterCache(lifetime, this);
            myCallbacks = new Dictionary<FileSystemPath, Action>();
        }

        public void RegisterDataChangedCallback(Lifetime lifetime, FileSystemPath projectLocation, Action action)
        {
            myCallbacks.Add(lifetime, projectLocation, action);
        }

        public bool IsLangVersionExplicitlySpecified(IProject project)
        {
            return myCache.GetData(this, project, ImplicitLangVersion).ExplicitlySpecified;
        }

        public bool CanHandle(FileSystemPath projectFileLocation)
        {
            using (ReadLockCookie.Create())
            {
                foreach (var projectItem in mySolution.FindProjectItemsByLocation(projectFileLocation))
                {
                    var projectFile = projectItem as IProjectFile;
                    if (projectFile?.GetProject()?.ProjectProperties.BuildSettings is CSharpBuildSettings)
                        return true;
                }
                return false;
            }
        }

        public int Version => 1;

        public LangVersionCache Read(FileSystemPath projectFileLocation, BinaryReader reader)
        {
            var explicitlySpecified = reader.ReadBoolean();
            return new LangVersionCache(explicitlySpecified);
        }

        public void Write(FileSystemPath projectFileLocation, BinaryWriter writer, LangVersionCache data)
        {
            writer.Write(data.ExplicitlySpecified);
        }

        public LangVersionCache BuildData(FileSystemPath projectFileLocation, XmlDocument document)
        {
            var documentElement = document.DocumentElement;
            if (documentElement == null || documentElement.Name != "Project")
                return ImplicitLangVersion;

            foreach (var propertyGroup in documentElement.GetElementsByTagName("PropertyGroup"))
            {
                var xmlElement = propertyGroup as XmlElement;
                if (xmlElement?.GetElementsByTagName("LangVersion").Count > 0)
                {
                    // We don't care if this is a conditional element or not. We only
                    // care if it's explicitly in the file. If it is, then VSTU wrote
                    // it, and we should honour it (even if it's conditional). If it's
                    // not there, then the file is from before VSTU started writing it,
                    // and we should handle that accordingly (i.e. override language
                    // level to C#5)
                    return ExplicitLangVersion;
                }
            }

            return ImplicitLangVersion;
        }

        public Action OnDataChanged(FileSystemPath projectFileLocation, LangVersionCache oldData, LangVersionCache newData)
        {
            Action action;
            myCallbacks.TryGetValue(projectFileLocation, out action);
            return action;
        }
    }

    public class LangVersionCache
    {
        public LangVersionCache(bool explicitlySpecified)
        {
            ExplicitlySpecified = explicitlySpecified;
        }

        public bool ExplicitlySpecified { get; }
    }
}