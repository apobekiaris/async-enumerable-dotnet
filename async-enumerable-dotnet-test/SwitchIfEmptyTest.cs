// Copyright (c) David Karnok & Contributors.
// Licensed under the Apache 2.0 License.
// See LICENSE file in the project root for full license information.

using Xunit;
using async_enumerable_dotnet;

namespace async_enumerable_dotnet_test
{
    public class SwitchIfEmptyTest
    {
        [Fact]
        public async void NonEmpty()
        {
            await AsyncEnumerable.Range(1, 5)
                .SwitchIfEmpty(AsyncEnumerable.Range(11, 5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public async void Empty()
        {
            await AsyncEnumerable.Empty<int>()
                .SwitchIfEmpty(AsyncEnumerable.Range(11, 5))
                .AssertResult(11, 12, 13, 14, 15);
        }
    }
}
