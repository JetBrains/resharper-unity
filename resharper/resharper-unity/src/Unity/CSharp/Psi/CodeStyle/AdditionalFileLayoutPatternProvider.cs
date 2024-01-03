using System;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CSharp.FileLayout;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
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
            var solution = declaration.GetSolution();
            if (!solution.HasUnityReference())
                return null;

            // TODO: This doesn't work with ReSharper - the resources haven't been added
            // If we add them, how do we edit them?

            try
            {
                var pattern = store.GetValue((AdditionalFileLayoutSettings s) => s.Pattern);
                return FileLayoutUtil.ParseFileLayoutPattern(solution, pattern);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }
    }
}