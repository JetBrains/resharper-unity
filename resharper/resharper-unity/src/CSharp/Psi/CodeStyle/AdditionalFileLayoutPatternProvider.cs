using System;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle
{
    [ShellComponent]
    public class AdditionalFileLayoutPatternProvider : IAdditionalCSharpFileLayoutPatternProvider
    {
        public Patterns GetPattern(IContextBoundSettingsStore store, ICSharpTypeAndNamespaceHolderDeclaration declaration)
        {
            if (!declaration.GetSolution().HasUnityReference())
                return null;

            try
            {
                var pattern = store.GetValue((AdditionalFileLayoutSettings s) => s.Pattern);
                return CSharpFormatterHelper.ParseFileLayoutPattern(pattern);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }
    }
}