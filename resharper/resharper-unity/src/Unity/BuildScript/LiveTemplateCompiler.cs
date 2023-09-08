using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Helpers;
using JetBrains.Application.BuildScript.PreCompile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.BuildScript;

public static class LiveTemplateCompiler
{
    private const string CompilerPackageName = "CitizenMatt.ReSharper.LiveTemplateCompiler";

    [BuildStep]
    public static async Task<Tuple<LocalPrepareWorkingCopy, CompileSubplatformsInput>> CompileLiveTemplates(AllAssembliesOnSources allAssembliesOnSources, LocalRestoredPackageReferenceArtifact[] restoredRefs, DotNetCoreSdkHelper sdkhelper, ILogger logger)
    {
        var unitySubsRoot = RelativePath.Parse("Plugins/ReSharperUnity");
        var unityProjects = allAssembliesOnSources.Subplatforms
            .Where(x => unitySubsRoot.IsPrefixOf(x.Name.RelativePath))
            .SelectMany(x =>
                x.ProjectFilesEx.Select(p => new SubplatformProjectWrapper(x, p, allAssembliesOnSources)));
        
        var liveTemplateItems = unityProjects.SelectMany(ExtractLiveTemplateItems).ToList();

        var restoredCompilePackage = restoredRefs
            .Where(x => x.RetrievedPackageManifest.Identity.Id == CompilerPackageName)
            .SingleOrFirstOrDefaultErr() ?? throw new InvalidOperationException($"Can't find {CompilerPackageName} package.");

        var compilerExec = allAssembliesOnSources.ProductHomeDir / restoredCompilePackage.RestoredLocation / "tools"
                           / "netcore" / "rstc.dll";
        var dotnetHost = await sdkhelper.GetDotnetHostPath();

        foreach (var liveTemplateItem in liveTemplateItems)
        {
            await RunCompiler(liveTemplateItem, compilerExec, dotnetHost, logger);
        }

        return new Tuple<LocalPrepareWorkingCopy, CompileSubplatformsInput>(LocalPrepareWorkingCopy.Item, new CompileSubplatformsInput());
    }

    private static Task RunCompiler(LiveTemplateItem liveTemplateItem, FileSystemPath compilerExec, FileSystemPath dotnetHost, ILogger logger)
    {
        var inputFiles = SearchFiles(liveTemplateItem.ProjectDir, RelativePath.Parse(liveTemplateItem.Include).PathWithCurrentPlatformSeparators());
        
        var startInfo = new InvokeChildProcess.StartInfo(dotnetHost)
        {
            Pipe = InvokeChildProcess.PipeStreams.IntoJetLogger(levelDefault: LoggingLevel.VERBOSE),
            CurrentDirectory = liveTemplateItem.ProjectDir,
            Arguments = new CommandLineBuilderJet().AppendFileName(compilerExec)
                .AppendSwitch("compile")
                .AppendSwitch("-i")
                .AppendSwitch(string.Join(" ", inputFiles
                    .Select(x => x.MakeRelativeTo(liveTemplateItem.ProjectDir))
                    .Select(x => x.PathWithCurrentPlatformSeparators().QuoteIfNeeded())))
                .AppendSwitch("-o")
                .AppendParameterWithQuoting(RelativePath.Parse(liveTemplateItem.OutputFile).PathWithCurrentPlatformSeparators())
                .AppendSwitch("-r")
                .AppendParameterWithQuoting(RelativePath.Parse(liveTemplateItem.ReadmeFile).PathWithCurrentPlatformSeparators())
                
        };
        logger.Verbose($"Starting \"\"{dotnetHost} {startInfo.Arguments.ToString()}\"\" in directory {liveTemplateItem.ProjectDir}");
        return Lifetime.UsingAsync
        (async lifetime =>
        {
            var result = await InvokeChildProcess.InvokeCore(lifetime, startInfo, InvokeChildProcess.SyncAsync.Async, logger);
            if (result != 0)
                throw new InvalidOperationException($"rstc.exe (LiveTemplateCompiler for Unity) exited with code {result}.");
        });
    }
    
    private static IEnumerable<LiveTemplateItem> ExtractLiveTemplateItems(SubplatformProjectWrapper projectFile)
    {
        var csprojPath = projectFile.GetProjectFileAbsPath();
        var xmldoc = new XmlDocument();
        var nsman = new XmlNamespaceManager(xmldoc.NameTable);
        nsman.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");
        try
        {
            csprojPath.ReadStream(xmldoc.Load);
        }
        catch(Exception ex)
        {
            throw new InvalidOperationException($"Can't load XML from project file {csprojPath.QuoteIfNeeded()}.", ex);
        }
        
        var liveTemplateItems = xmldoc.SelectElements("/msb:Project/msb:ItemGroup/msb:LiveTemplate", nsman);
        return liveTemplateItems.Select(x =>
        {
            var include = x.GetAttribute("Include");
            if (include.IsNullOrEmpty())
                throw new InvalidOperationException(
                    $"LiveTemplate item in project file {csprojPath.QuoteIfNeeded()} doesn't have Include attribute.");

            var outputFile = x.ChildElements().FirstOrDefault(y => y.Name == "OutputFile")?.InnerText;
            if (outputFile.IsNullOrEmpty())
                throw new InvalidOperationException(
                    $"LiveTemplate item in project file {csprojPath.QuoteIfNeeded()} doesn't have OutputFile element.");

            var readmeFile = x.ChildElements().FirstOrDefault(y => y.Name == "ReadmeFile")?.InnerText;
            if (readmeFile.IsNullOrEmpty())
                throw new InvalidOperationException(
                    $"LiveTemplate item in project file {csprojPath.QuoteIfNeeded()} doesn't have ReadmeFile element.");


            return new LiveTemplateItem(include, outputFile, readmeFile, projectFile.GetProjectFileAbsPath().Parent);
        });
    }

    private readonly record struct LiveTemplateItem(string Include, string OutputFile, string ReadmeFile,
        FileSystemPath ProjectDir);
    
    private static IEnumerable<FileSystemPath> SearchFiles(FileSystemPath root, string pattern)
    {
        var parts = pattern.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.None);

        return SearchRecursive(root, parts, 0);
    }

    private static IEnumerable<FileSystemPath> SearchRecursive(FileSystemPath root, string[] parts, int depth)
    {
        if (depth >= parts.Length) yield break;
        if (!root.ExistsDirectory) yield break;

        var part = parts[depth];

        if (part == "**")
        {
            foreach (var dir in root.GetDirectoryEntries("*", PathSearchFlags.RecurseIntoSubdirectories))
            {
                foreach (var file in SearchRecursive(dir.GetAbsolutePath(), parts, depth + 1))
                {
                    yield return file;
                }
            }
        }
        else
        {
            if (depth == parts.Length - 1) // Last part, looking for files
            {
                foreach (var file in root.GetChildFiles(mask: part))
                {
                    yield return file;
                }
            }
            else
            {
                foreach (var dir in root.GetDirectoryEntries(part))
                {
                    foreach (var file in SearchRecursive(dir.GetAbsolutePath(), parts, depth + 1))
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}