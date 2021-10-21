using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    public abstract class AsmDefProblemAnalyzer<T> : UnityElementProblemAnalyzerBase<T, JsonNewLanguage>
        where T : ITreeNode
    {
        protected override bool IsAcceptableFile(IPsiSourceFile sourceFile, IFile file) => sourceFile.IsAsmDef();

        // INFO severity highlights are supported for non-user code, primarily for identifier highlighting. We're using
        // it to show extended info such as tooltips, inlay hints and greyed out elements. We want this to work with
        // external .asmdef files, which are marked as non-user code, but we don't want them to work with arbitrary
        // .asmdef files dropped into the editor. This check makes sure we're working with a real project file, or one
        // of our known external files
        protected bool IsProjectFileOrKnownExternalFile(IPsiSourceFile? sourceFile, UnityExternalFilesPsiModule module)
        {
            if (sourceFile == null)
                return false;

            if (module.ContainsFile(sourceFile))
                return true;

            var projectFile = sourceFile.ToProjectFile();
            return projectFile != null && !projectFile.IsMiscProjectItem();
        }
    }
}
