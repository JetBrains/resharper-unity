using System;
using System.Linq;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Util;
using JetBrains.Util.Storage;

namespace JetBrains.ReSharper.Plugins.Unity.BuildScript
{
  public class CopyUnityAnnotations
  {
    [BuildStep]
    public static SubplatformFileForPackaging[] Run(AllAssembliesOnEverything allass, ProductHomeDirArtifact homedir)
    {
      if (allass.FindSubplatformByClass<CopyUnityAnnotations>() is SubplatformOnSources subplatform)
      {
        FileSystemPath dirAnnotations = homedir.ProductHomeDir / subplatform.Name.RelativePath / "annotations";
        return dirAnnotations.GetChildFiles().Select(CopyFileToOutputRequest).ToArray();

        SubplatformFileForPackaging CopyFileToOutputRequest(FileSystemPath path)
        {
          return new SubplatformFileForPackaging(
            subplatform.Name,
            ImmutableFileItem.CreateFromDisk(path).WithRelativePath((RelativePath)"Extensions" / "com.intellij.resharper.unity" / "annotations" / path.Name));
        }
      }

      return Array.Empty<SubplatformFileForPackaging>();
    }
  }
}