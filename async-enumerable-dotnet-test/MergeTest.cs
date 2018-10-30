// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using System.Threading.Tasks;
using async_enumerable_dotnet;
using System;
using System.Collections.Generic;

namespace async_enumerable_dotnet_test
{
    public class MergeTest
    {
        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Merge<int>(
                )
                .AssertResult();
        }

        [Fact]
        public async void Solo()
        {
            await AsyncEnumerable.Merge(
                    AsyncEnumerable.Range(1, 5)
                )
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Normal()
        {
            await AsyncEnumerable.Merge(
                    AsyncEnumerable.Range(1, 5),
                    AsyncEnumerable.Range(6, 5)
                )
                .AssertResultSet(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void Normal_Uneven_1()
        {
            await AsyncEnumerable.Merge(
                    AsyncEnumerable.Range(1, 5),
                    AsyncEnumerable.Range(6, 4)
                )
                .AssertResultSet(1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

        [Fact]
        public async void Normal_Uneven_2()
        {
            await AsyncEnumerable.Merge(
                    AsyncEnumerable.Range(1, 4),
                    AsyncEnumerable.Range(6, 5)
                )
                .AssertResultSet(1, 2, 3, 4, 6, 7, 8, 9, 10);
        }

        [Fact]
        public async void Error()
        {
            await AsyncEnumerable.Merge(
                    AsyncEnumerable.Range(1, 5),
                    AsyncEnumerable.Error<int>(new InvalidOperationException())
                )
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Push()
        {
            for (var i = 0; i < 10; i++)
            {
                var push = new MulticastAsyncEnumerable<int>();

                var en = AsyncEnumerable.Merge(
                        push.Filter(v => v % 2 == 0), 
                        push.Filter(v => v % 2 != 0)
                    )
                    .ToListAsync();

                var t = Task.Run(async () =>
                {
                    for (var j = 0; j < 100_000; j++)
                    {
                        await push.Next(j);
                    }
                    await push.Complete();
                });

                var list = await en;

                await t;

                var set = new HashSet<int>(list);

                Assert.Equal(100_000, set.Count);
            }
        }

        [Fact]
        public async void Multicast_Merge()
        {
            for (var i = 0; i < 100000; i++)
            {
                await AsyncEnumerable.Range(1, 5)
                    .Publish(a => a.Take(3).MergeWith(a.Skip(3)))
                    .AssertResultSet(1, 2, 3, 4, 5);
            }
        }

    }
}