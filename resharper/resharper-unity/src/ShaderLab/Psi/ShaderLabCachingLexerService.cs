using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi
{
  [Language(typeof(ShaderLabLanguage))]
  public class ShaderLabCachingLexerService : UniversalCachingLexerService
  {
    public override CachingLexer Resync(ISolution solution, DocumentUpdatesAccumulator updatesAccumulator, ProjectFileType projectFileType,
      ITextControl textControl, PsiProjectFileTypeCoordinator projectFileTypeCoordinator, IPsiSourceFile sourceFile = null)
    {
      // TOdo resync via TokenBuffer.Resync, cache TokenBuffer.
      return new ShaderLabLexerGenerated(textControl.Document.Buffer, CppLexer.Create).ToCachingLexer().TokenBuffer.CreateLexer();
    }

    public override CachingLexer CreateCachingLexer(ISolution solution, ProjectFileType projectFileType, ITextControl textControl,
      PsiProjectFileTypeCoordinator projectFileTypeCoordinator, IPsiSourceFile sourceFile = null)
    {
      return new ShaderLabLexerGenerated(textControl.Document.Buffer, CppLexer.Create).ToCachingLexer().TokenBuffer.CreateLexer();
    }

    public override void Drop(ITextControl textControl)
    {
    }
  }
}