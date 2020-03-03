using System;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredCachesLocks
    {
        private Lifetime myLifetime;

        private readonly SequentialLifetimes mySequentialLifetimes;
        private readonly IShellLocks myShellLocks;
        private readonly IDaemonThread myDaemonThread;
        private readonly ReaderWriterLockSlim myReaderWriterLockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public DeferredCachesLocks(Lifetime lifetime, IShellLocks shellLocks, IDaemonThread daemonThread)
        {
            mySequentialLifetimes = new SequentialLifetimes(lifetime);
            myLifetime = mySequentialLifetimes.Next();
            myShellLocks = shellLocks;
            myDaemonThread = daemonThread;
        }

        public void ExecuteUnderWriteLock(Action action)
        {
            Assertion.Assert(myShellLocks.IsWriteAccessAllowed() || myDaemonThread.IsDaemonThread, "WriteLock expected or myDaemonThread.IsDaemonThread");
            myLifetime = mySequentialLifetimes.Next();
            myReaderWriterLockSlim.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                myReaderWriterLockSlim.ExitWriteLock();
            }
        }

        public void ExecuteUnderReadLock(Action<Lifetime> action)
        {
            myShellLocks.AssertReadAccessAllowed();
            myReaderWriterLockSlim.EnterReadLock();
            try
            {
                Thread.MemoryBarrier();
                action(myLifetime);
            }
            finally
            {
                myReaderWriterLockSlim.ExitReadLock();
            }
        }
        
        public T ExecuteUnderReadLock<T>(Func<Lifetime, T> action)
        {
            myShellLocks.AssertReadAccessAllowed();
            myReaderWriterLockSlim.EnterReadLock();
            try
            {
                Thread.MemoryBarrier();
                return action(myLifetime);
            }
            finally
            {
                myReaderWriterLockSlim.ExitReadLock();
            }
        }
        
        [Conditional("JET_MODE_ASSERT")]
        public void AssertReadAccessAllowed()
        {
            Logger.Assert(myReaderWriterLockSlim.IsReadLockHeld, "This operation requires a deferred caches read lock");
        }
        
        [Conditional("JET_MODE_ASSERT")]
        public void AssertWriteAccessAllowed()
        {
            Logger.Assert(myShellLocks.IsWriteAccessAllowed() || myReaderWriterLockSlim.IsWriteLockHeld, "This operation requires a deferred caches write lock or standard write lock");
        }
    }
}