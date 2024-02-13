using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Build.Helpers.TeamCity;
using JetBrains.Util;
using JetBrains.Util.Storage;

namespace JetBrains.ReSharper.Plugins.Unity.BuildScript
{
  public class CopyUnityAnnotations
  {
    [BuildStep]
    public static SubplatformFileForPackagingFast[] Run(AllAssembliesOnEverything allass, ProductHomeDirArtifact homedir)
    {
      if (allass.FindSubplatformByClass<CopyUnityAnnotations>() is SubplatformOnSources subplatform)
      {
        FileSystemPath dirAnnotations = homedir.ProductHomeDir / subplatform.Name.RelativePath / "annotations";
        return dirAnnotations.GetChildFiles().SelectMany(CopyFileToOutputRequest).ToArray();

        IEnumerable<SubplatformFileForPackagingFast> CopyFileToOutputRequest(FileSystemPath path)
        {
          yield return new SubplatformFileForPackagingFast(
            subplatform.Name,
            ImmutableFileItem.CreateFromDisk(path).WithRelativePath((RelativePath)"Extensions" / "com.intellij.resharper.unity" / "annotations" / path.Name));

          // see ExtensionsExternalAnnotationsFileProvider
          // That HACK required only for local case. We should fix how we mount Plugin in local dev envrinment.
          
          // When we are running Rider from resharper-unity github repo (via SDK) or when we are running Rider from installer
          // plugin is not installed as Subplatform, it is mounted as IntelliJ plugin (see JET_ADDITIONAL_DEPLOYED_PACKAGES_FILE usage) with com.intellij.resharper.unity
          // In local dev environment we are extracting backend part of Unity plugin to Bin.Directory and it is mounted as JetBrains.Plugins.ReSharperUnity.resharper.resharper-unity.src.Unity
          // in ApplicationPackagesFiles 
          if (!TeamCityProperties.GetIsRunningInTeamCity())
          {
              yield return new SubplatformFileForPackagingFast(
                  subplatform.Name,
                  ImmutableFileItem.CreateFromDisk(path).WithRelativePath((RelativePath)"Extensions" /
                                                                          "JetBrains.Plugins.ReSharperUnity.resharper.resharper-unity.src.Unity" /
                                                                          "annotations" / path.Name));
          }
        }
      }

      return Array.Empty<SubplatformFileForPackagingFast>();
    }
  }
}