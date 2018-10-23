﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace async_enumerable_dotnet.impl
{
    internal sealed class FromObservable<T> : IAsyncEnumerable<T>
    {
        readonly IObservable<T> source;

        public FromObservable(IObservable<T> source)
        {
            this.source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var consumer = new FromObservableEnumerator();
            var d = source.Subscribe(consumer);
            consumer.SetDisposable(d);
            return consumer;
        }

        internal sealed class FromObservableEnumerator : IAsyncEnumerator<T>, IObserver<T>, IDisposable
        {
            readonly ConcurrentQueue<T> queue;

            volatile bool done;
            Exception error;

            IDisposable upstream;

            public T Current => current;

            T current;

            TaskCompletionSource<bool> resume;

            long wip;

            public FromObservableEnumerator()
            {
                this.queue = new ConcurrentQueue<T>();
            }

            public void Dispose()
            {
                // deliberately no-op as `this` is the terminal indicator
            }

            public ValueTask DisposeAsync()
            {
                current = default;
                Interlocked.Exchange(ref upstream, this)?.Dispose();
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var d = done;
                    var success = queue.TryDequeue(out var v);

                    if (d && !success)
                    {
                        if (error != null)
                        {
                            throw error;
                        }
                        return false;
                    }
                    else
                    if (success)
                    {
                        current = v;
                        return true;
                    }

                    if (Volatile.Read(ref wip) == 0)
                    {
                        await ResumeHelper.Resume(ref resume).Task;
                    }
                    ResumeHelper.Clear(ref resume);
                    Interlocked.Exchange(ref wip, 0);
                }
            }

            public void OnCompleted()
            {
                this.done = true;
                Signal();
            }

            public void OnError(Exception error)
            {
                this.error = error;
                this.done = true;
                Signal();
            }

            public void OnNext(T value)
            {
                queue.Enqueue(value);
                Signal();
            }

            internal void SetDisposable(IDisposable d)
            {
                if (Interlocked.CompareExchange(ref upstream, d, null) != null)
                {
                    d?.Dispose();
                } 
            }

            void Signal()
            {
                if (Interlocked.Increment(ref wip) == 1L)
                {
                    ResumeHelper.Resume(ref resume).TrySetResult(true);
                }
            }
        }
    }
}
