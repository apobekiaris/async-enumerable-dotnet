﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class CreateEmitter<T> : IAsyncEnumerable<T>
    {
        readonly Func<IAsyncEmitter<T>, Task> handler;

        public CreateEmitter(Func<IAsyncEmitter<T>, Task> handler)
        {
            this.handler = handler;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = new CreateEmitterEnumerator();
            en.SetTask(handler(en));
            return en;
        }

        internal sealed class CreateEmitterEnumerator : IAsyncEnumerator<T>, IAsyncEmitter<T>
        {
            Task task;

            volatile bool disposeRequested;

            volatile bool taskComplete;

            public bool DisposeAsyncRequested => disposeRequested;

            public T Current => current;

            TaskCompletionSource<bool> valueReady;

            TaskCompletionSource<bool> consumed;

            T current;

            internal void SetTask(Task task)
            {
                this.task = task;
                task.ContinueWith(t =>
                {
                    taskComplete = true;
                    ResumeHelper.Resume(ref valueReady);
                });
            }

            public ValueTask DisposeAsync()
            {
                disposeRequested = true;
                ResumeHelper.Resume(ref consumed);
                return new ValueTask(task);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (taskComplete)
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }
                    return false;
                }
                ResumeHelper.Resume(ref consumed);

                await ResumeHelper.Await(ref valueReady);
                ResumeHelper.Clear(ref valueReady);

                if (!taskComplete)
                {
                    return true;
                }
                else if (task.IsFaulted)
                {
                    throw task.Exception;
                }
                return false;
            }

            public async ValueTask Next(T value)
            {
                if (disposeRequested)
                {
                    return;
                }
                await ResumeHelper.Await(ref consumed);
                ResumeHelper.Clear(ref consumed);
                if (disposeRequested)
                {
                    return;
                }

                current = value;

                ResumeHelper.Resume(ref valueReady);
            }
        }
    }
}
