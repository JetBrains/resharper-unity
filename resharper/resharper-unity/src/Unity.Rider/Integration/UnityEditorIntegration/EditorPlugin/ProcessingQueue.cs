using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.EditorPlugin
{
    internal sealed class ProcessingQueue
    {
        private readonly Lifetime myLifetime;
        private readonly IShellLocks myLocks;
        private readonly object mySync = new();
        private readonly Queue<Action> myElements = new();
        private bool myIsActive;

        public ProcessingQueue(IShellLocks locks, Lifetime lifetime)
        {
            myLocks = locks;
            myLifetime = lifetime;
        }

        public void Enqueue(Action request)
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
                if (myLifetime.IsNotAlive)
                    break;

                Task task;
                lock(mySync)
                {
                    if (myElements.IsEmpty())
                    {
                        myIsActive = false;
                        break;
                    }

                    task = Task.Run(myElements.Dequeue(), myLifetime);
                }

                await task;
            }
        }
    }
}