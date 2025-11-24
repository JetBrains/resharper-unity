using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Application.Components;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.ActionSystem.Text;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using JetBrains.ReSharper.TestFramework.Components.Psi;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Collections;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Yaml.Psi.Parsing
{
  [Category("IncrementalReparse")]
  public abstract class IncrementalReparseTestBase : BaseTestWithTextControl
  {
    protected virtual bool CompareNodes(ITreeNode node1, ITreeNode node2)
    {
      return ReferenceEquals(node1.GetType(), node2.GetType());
    }

    private void CompareTrees(ITreeNode? tree1, ITreeNode? tree2)
    {
      Assert.IsNotNull(tree1, "tree1 == null");
      Assert.IsNotNull(tree2, "tree2 == null");
      Assert.AreEqual(tree1!.GetText(), tree2!.GetText(), "Tree text mismatch");
      Assert.IsTrue(CompareNodes(tree1, tree2), "Tree element types mismatch: {0} != {1}", tree1.GetType(), tree2.GetType());

      var child1 = tree1.FirstChild;
      var child2 = tree2.FirstChild;
      while (child1 != null || child2 != null)
      {
        CompareTrees(child1, child2);
        child1 = child1!.NextSibling;
        child2 = child2!.NextSibling;
      }
    }

    private static IDictionary<PsiLanguageType, IFile> GetPsiFiles(IPsiSourceFile sourceFile)
    {
      var psiFiles = sourceFile.GetSolution().GetPsiServices().Files;
      psiFiles.CommitAllDocuments();

      var files = new SortedDictionary<PsiLanguageType, IFile>(
        sourceFile.GetPsiServices().Files.GetPsiFiles(sourceFile).ToDictionary(file => file.Language),
        Comparer.Create<PsiLanguageType>((lang1, lang2) => string.CompareOrdinal(lang1.Name, lang2.Name)));
      Assert.IsNotEmpty(files, "Psi files must exist for {0}", sourceFile);

      // check references validity
      foreach (var pair in files)
      {
        AssertReferencesAndDeclaredElementsAreValid(pair.Value);
      }
      return files;
    }

    private static void AssertReferencesAndDeclaredElementsAreValid(ITreeNode treeNode)
    {
      // references
      foreach (var reference in treeNode.GetReferences())
      {
        Assert.IsTrue(reference.IsValid(),
          "Invalid reference: {0}, node type:{1}, node text:{2}", reference.GetType().FullName, treeNode.GetType().FullName, treeNode.GetText());
      }

      // declared element
      if (treeNode is IDeclaration declaration)
      {
        var declaredElement = declaration.DeclaredElement;
        if (declaredElement != null)
          Assert.IsTrue(declaredElement.IsValid(), "Declared eleemnts is invalid for {0}", declaration.GetText());
      }

      // recursive call
      for (var child = treeNode.FirstChild; child != null; child = child.NextSibling)
      {
        AssertReferencesAndDeclaredElementsAreValid(child);
      }
    }

    protected override void DoTest(Lifetime lifetime, IProject testProject)
    {
      var positionsToCheck = GetCaretPositions().DefaultIfEmpty(GetCaretPosition()).ToList();
      Assert.IsNotEmpty(positionsToCheck, "Nothing to check - put {caret} where necessary");

      var reparsedNodes = new List<ITreeNode>();

      ShellInstance.GetComponent<TestIdGenerator>().Reset();
      var psiFiles = Solution.GetPsiServices().Files;

      void PsiChanged(ITreeNode? node, PsiChangedElementType type)
      {
        if (node != null) reparsedNodes.Add(node);
      }

      psiFiles.AfterPsiChanged += PsiChanged;

      try
      {
        var textControl = OpenTextControl(lifetime);
        {
          using (CompilationContextCookie.GetOrCreate(testProject.GetResolveContext()))
          {
            // number of original files
            var originalFiles = new Dictionary<IPsiSourceFile, int>();
            foreach (var caretPosition in positionsToCheck)
            {
              var projectFile = GetProjectFile(testProject, caretPosition.FileName);
              Assert.NotNull(projectFile);

              foreach (var psiSourceFile in projectFile.ToSourceFiles())
              {
                originalFiles.Add(psiSourceFile, GetPsiFiles(psiSourceFile).Count);
              }
            }

            var checkAll = GetSetting(textControl.Document.Buffer, "CHECKALL");

            // change text
            var actions = GetSettings(textControl.Document.Buffer, "ACTION");
            if (actions.Count == 0)
              throw new Exception("No actions found");

            foreach (var action in actions)
            {
              if (action.Length == 0)
                continue;

              var text = action.Substring(1).Replace("{LEFT}", "{").Replace("{RIGHT}", "}");
              switch (action.ToCharArray()[0])
              {
                case '+':
                  textControl.Document.InsertText(textControl.Caret.DocumentOffset(), text);
                  break;
                case '-':
                  textControl.Document.DeleteText(TextRange.FromLength(textControl.Caret.Offset(), Convert.ToInt32(text)));
                  break;
                case '>':
                  textControl.Caret.MoveTo(textControl.Caret.Offset() + Convert.ToInt32(text),
                    CaretVisualPlacement.Generic);
                  break;
                case '<':
                  textControl.Caret.MoveTo(textControl.Caret.Offset() - Convert.ToInt32(text),
                    CaretVisualPlacement.Generic);
                  break;
                default:
                  var actionManager = ShellInstance.GetComponent<IActionManager>();
                  actionManager.Defs.GetActionDefById(TextControlActions.Composition.Compose(action, false))
                    .EvaluateAndExecute(actionManager);
                  break;
              }

              if (String.Equals(checkAll, "true", StringComparison.InvariantCultureIgnoreCase))
              {
                foreach (var data in originalFiles)
                {
                  GetPsiFiles(data.Key);
                }
              }
            }

            foreach (var data in originalFiles)
            {
              var psiSourceFile = data.Key;

              Assert.IsTrue(psiSourceFile.IsValid());

              // get reparsed files
              var reparsedFiles = GetPsiFiles(psiSourceFile);

              Assert.AreEqual(reparsedFiles.Count, data.Value, "Reparsed psi files count mismatch for {0}", psiSourceFile);

              // check reparsed element
              ExecuteWithGold(psiSourceFile, writer =>
              {
                if (reparsedNodes.IsEmpty())
                {
                  writer.Write("Fully reparsed");
                }
                else
                {
                  reparsedNodes.Sort((n1, n2) => String.CompareOrdinal(n1.Language.Name, n2.Language.Name));
                  foreach (var reparsedNode in reparsedNodes)
                  {
                    if (reparsedNode is IFile)
                    {
                      writer.WriteLine("{0}: Fully reparsed", reparsedNode.Language);
                    }
                    else
                    {
                      var nodeType = reparsedNode.GetType();
                      writer.WriteLine("{0}: reparsed node type: {1}, text: {2}", reparsedNode.Language,
                        PresentNodeType(nodeType), reparsedNode.GetText());
                    }
                  }
                }
                if (DoDumpRanges)
                {
                  DumpRanges<PsiLanguageType>(psiSourceFile, writer);
                }
              });

              // drop psi files cache
              WriteLockCookie.Execute(() => psiFiles.MarkAsDirty(psiSourceFile));

              var files = GetPsiFiles(psiSourceFile);
              Assert.AreEqual(files.Count, reparsedFiles.Count, "Psi files count mismatch");

              foreach (var pair in files)
              {
                var language = pair.Key;
                Assert.IsTrue(reparsedFiles.TryGetValue(language, out var reparsedFile), "Failed to find psi file for {0}", language);

                CompareTrees(pair.Value, reparsedFile);
              }
            }
          }
        }
      }
      finally
      {
        psiFiles.AfterPsiChanged -= PsiChanged;
        reparsedNodes.Clear();
      }
    }

    protected virtual bool DoDumpRanges => false;

    private static string PresentNodeType(Type type)
    {
      if (!type.IsGenericType)
        return type.FullName!;

      var sb = new StringBuilder();
      sb.AppendFormat("{0}.{1}", type.Namespace, type.Name);
      sb.Append("[");
      bool isFirst = true;
      foreach (var argument in type.GetGenericArguments())
      {
        if (isFirst)
          isFirst = false;
        else
        {
          sb.Append(", ");
        }

        sb.Append(PresentNodeType(argument));
      }
      sb.Append("]");
      return sb.ToString();
    }
  }
}
