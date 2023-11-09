#nullable enable
using System;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Psi.Files;

public static class PsiFilesEx
{
    public static void StartMainReadOnCommitAvoidingWriteLockAsync(this IPsiFiles psiFiles, Lifetime lifetime, Action action)
    {
        var locks = psiFiles.Solution.Locks;
        if (locks.IsOnMainThreadGuarded() && psiFiles.AllDocumentsAreCommitted)
        {
            action();
            return;
        }

        if (locks.IsWriteLockHeld())
            locks.Queue(lifetime, "ShaderVariantHost.Commit", CommitAsync);
        else
            CommitAsync();
        return;

        void CommitAsync()
        {
            using (ReadLockCookie.Create()) 
                psiFiles.CommitAllDocumentsAsync(() => lifetime.Execute(action));
        }
    }
}