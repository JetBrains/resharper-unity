using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.DataFlow;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    internal sealed class ProcessingQueue
    {
        private readonly Lifetime myLifetime;
        private readonly IShellLocks myLocks;

        private readonly object mySync = new object();

        private readonly Queue<Action> myElements = new Queue<Action>();
        private bool myIsActive;

        public ProcessingQueue(IShellLocks locks, Lifetime lifetime)
        {
            myLocks = locks;
            myLifetime = lifetime;
        }

        public void Enqueue([NotNull] Action request)
        {
            lock(mySync)
            {
                myElements.Enqueue(request);
                if (myIsActive)
                    return;

                myIsActive = true;
            }

            myLocks.Tasks.StartNew(myLifetime, Scheduling.FreeThreaded, ProcessAllAsync);
        }

        private async Task ProcessAllAsync()
        {
            while (true)
            {
                if (myLifetime.IsTerminated)
                    break;

                Task task;
                lock(mySync)
                {
                    if (myElements.IsEmpty())
                    {
                        myIsActive = false;
                        break;
                    }

                    task = Task.Run(myElements.Dequeue());
                }

                await task;
            }
        }
    }
}