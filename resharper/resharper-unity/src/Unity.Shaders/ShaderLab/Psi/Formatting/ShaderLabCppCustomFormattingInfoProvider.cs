using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting
{
  [ProjectFileType(typeof(ShaderLabProjectFileType))]
  public class ShaderLabCppCustomFormattingInfoProvider : ICppCustomFormattingInfoProvider
  {
    public bool DefaultAdjustLeadingAndTrailingWhitespaces => false;
    public void AdjustLeadingAndTrailingWhitespaces(CppCodeFormatter cppCodeFormatter, CppFile cppFile)
    {
      var cgProgram = (cppFile.Parent as IInjectedFileHolder)?.OriginalNode.PrevSibling;
        
      var s = ShaderLabCppFormatterExtension.GetIndentInCgProgram(cgProgram);
      cppCodeFormatter.RemoveLeadingSpacesInFile(cppFile);
      cppCodeFormatter.RemoveTrailingSpaces( cppFile);
      
      var lineEnding = cppFile.DetectLineEnding(cppFile.GetPsiServices());
      LowLevelModificationUtil.AddChildBefore(cppFile.firstChild, cppCodeFormatter.CreateNewLine(lineEnding), cppCodeFormatter.CreateSpace(s, null));
      LowLevelModificationUtil.AddChildAfter(cppFile.lastChild, cppCodeFormatter.CreateNewLine(lineEnding), cppCodeFormatter.CreateSpace(s, null));
    }
  }
}