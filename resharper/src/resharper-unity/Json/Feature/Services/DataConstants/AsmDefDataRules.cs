using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Util;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.DataConstants
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

        private ICollection<IDeclaredElement> GetDeclaredElementsFromContext(IDataContext dataContext)
        {
            var psiEditorView = dataContext.GetData(PsiDataConstants.PSI_EDITOR_VIEW);
            if (psiEditorView == null) return null;

            var psiView = psiEditorView.DefaultSourceFile.View<JsonLanguage>();
            foreach (var containingNode in psiView.ContainingNodes)
            {
                var sourceFile = containingNode.GetSourceFile();
                if (!sourceFile.IsAsmDef())
                    continue;

                if (containingNode.IsNameStringLiteralValue())
                {
                    return new List<IDeclaredElement>
                    {
                        new AsmDefNameDeclaredElement(containingNode.GetJavaScriptServices(),
                            containingNode.GetUnquotedText(), sourceFile,
                            containingNode.GetTreeStartOffset().Offset)
                    };
                }
            }

            return EmptyList<IDeclaredElement>.Instance;
        }
    }
}