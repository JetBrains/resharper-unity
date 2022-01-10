using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Shared;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TestFramework;
using JetBrains.ReSharper.TestFramework.Components.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Yaml.Psi.Parsing
{
  // This is a replacement for the standard ParserTestBase<TLanguage> that will check the nodes of the parsed tree
  // against the gold file, but will also assert that all top level chameleons are closed by default and open correctly.
  // It also asserts that each node correctly matches the textual content of the original document
  [Category("Parser")]
  public abstract class ParserTestBase<TLanguage> : BaseTestWithTextControl
    where TLanguage : PsiLanguageType
  {
    protected override void DoTest(Lifetime lifetime, IProject testProject)
    {
      ShellInstance.GetComponent<TestIdGenerator>().Reset();
      var textControl = OpenTextControl(lifetime);
      {
        ExecuteWithGold(textControl.Document, sw =>
        {
          var files = textControl
            .Document
            .GetPsiSourceFiles(Solution)
            .SelectMany(s => s.GetPsiFiles<TLanguage>())
            .ToList();
          files.Sort((file1, file2) => String.Compare(file1.Language.Name, file2.Language.Name, StringComparison.Ordinal));
          foreach (var psiFile in files)
          {
            // Assert all chameleons are closed by default
            var chameleons = psiFile.ThisAndDescendants<IChameleonNode>();
            while (chameleons.MoveNext())
            {
              var chameleonNode = chameleons.Current;
              if (chameleonNode.IsOpened && !(chameleonNode is IComment))
                Assertion.Fail("Found chameleon node that was opened after parser is invoked: '{0}'", chameleonNode.GetText());

              chameleons.SkipThisNode();
            }

            // Dump the PSI tree, opening all chameleons
            sw.WriteLine("Language: {0}", psiFile.Language);
            DebugUtil.DumpPsi(sw, psiFile);
            sw.WriteLine();
            if (((IFileImpl) psiFile).SecondaryRangeTranslator is RangeTranslatorWithGeneratedRangeMap rangeTranslator)
              WriteCommentedText(sw, "//", rangeTranslator.Dump(psiFile));

            // Verify textual contents
            var originalText = textControl.Document.GetText();
            Assert.AreEqual(originalText, psiFile.GetText(), "Reconstructed text mismatch");
            CheckRange(originalText, psiFile);
          }
        });
      }
    }

    private static void CheckRange([NotNull] string documentText, [NotNull] ITreeNode node)
    {
      Assert.AreEqual(node.GetText(), documentText.Substring(node.GetTreeStartOffset().Offset, node.GetTextLength()),
        "node range text mismatch");

      for (var child = node.FirstChild; child != null; child = child.NextSibling) CheckRange(documentText, child);
    }
  }
}