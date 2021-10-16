using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.DataConstants
{
    // This enables Find Usages on the string literal value for the "name" JSON property. The string literal value
    // doesn't have an IDeclaration, so the default rules can't find any IDeclaredElements. We have to provide one
    [ShellComponent]
    public class AsmDefDataRules
    {
        public AsmDefDataRules(Lifetime lifetime, IActionManager actionManager)
        {
            actionManager.DataContexts.RegisterDataRule(lifetime, "AsmDefDeclaredElements",
                PsiDataConstants.DECLARED_ELEMENTS, GetDeclaredElementsFromContext);
        }

        private static ICollection<IDeclaredElement> GetDeclaredElementsFromContext(IDataContext dataContext)
        {
            var psiEditorView = dataContext.GetData(PsiDataConstants.PSI_EDITOR_VIEW);
            if (psiEditorView == null) return null;

            var psiView = psiEditorView.DefaultSourceFile.View<JsonNewLanguage>();
            foreach (var containingNode in psiView.ContainingNodes)
            {
                var sourceFile = containingNode.GetSourceFile();
                if (sourceFile == null || !sourceFile.IsAsmDef())
                    continue;

                if (containingNode.IsNamePropertyValue())
                {
                    var node = (containingNode as IJsonNewLiteralExpression).NotNull("node != null");
                    return new List<IDeclaredElement>
                    {
                        new AsmDefNameDeclaredElement(node.GetStringValue(), sourceFile,
                            containingNode.GetTreeStartOffset().Offset)
                    };
                }
            }

            return null;
        }
    }
}