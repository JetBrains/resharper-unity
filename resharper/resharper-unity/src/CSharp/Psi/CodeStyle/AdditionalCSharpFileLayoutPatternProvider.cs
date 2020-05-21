using System;
using System.IO;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp.Impl.CodeStyle.MemberReordering;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle
{
    [ShellComponent]
    public class AdditionalCSharpFileLayoutPatternProvider : IAdditionalCSharpFileLayoutPatternProvider
    {
        public Patterns GetPattern(IContextBoundSettingsStore store, ICSharpTypeAndNamespaceHolderDeclaration declaration)
        {
            if (!declaration.GetSolution().HasUnityReference())
                return null;

            // TODO: Read this from settings, set up default value in settings from stream
            try
            {
                var ns = GetType().Namespace;
                using (var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(ns + ".UnityCodeCleanupPatterns.xaml"))
                {
                    Assertion.AssertNotNull(stream, "stream != null");
                    using (var reader = new StreamReader(stream))
                    {
                        var pattern = reader.ReadToEnd();
                        return CSharpFormatterHelper.ParseFileLayoutPattern(pattern);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }
    }
}