using System;
using System.Threading;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredCachesLocks
    {
        private Lifetime myLifetime;

        private readonly SequentialLifetimes mySequentialLifetimes;
        private readonly IShellLocks myShellLocks;
        private readonly IDaemonThread myDaemonThread;
        private readonly ReaderWriterLockSlim myReaderWriterLockSlim = new ReaderWriterLockSlim();

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
    }
}