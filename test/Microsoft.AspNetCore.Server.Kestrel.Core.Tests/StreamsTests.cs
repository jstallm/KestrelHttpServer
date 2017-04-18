// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class StreamsTests
    {
        [Fact]
        public void UpgradesRequestAndResponse()
        {
            var streams = new Streams(Mock.Of<IFrameControl>());
            streams.Start(null);

            var upgrade = Assert.IsType<FrameDuplexStream>(streams.Upgrade());
            Assert.True(streams.RequestBody.Upgraded);
            Assert.True(streams.ResponseBody.Upgraded);
        }

        [Fact]
        public async Task Abort()
        {
            var streams = new Streams(Mock.Of<IFrameControl>());
            streams.Start(null);

            var ex = new Exception("My error");
            streams.Abort(ex);

            await streams.ResponseBody.WriteAsync(new byte[1], 0, 1);
            Assert.Same(ex,
                await Assert.ThrowsAsync<Exception>(() => streams.RequestBody.ReadAsync(new byte[1], 0, 1)));
        }

        [Fact]
        public async Task AbortAfterUpgrade()
        {
            var streams = new Streams(Mock.Of<IFrameControl>());
            streams.Start(null);

            var upgrade = streams.Upgrade();
            var ex = new Exception("My error");
            streams.Abort(ex);

            var writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => streams.ResponseBody.WriteAsync(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, writeEx.Message);

            Assert.Same(ex,
              await Assert.ThrowsAsync<Exception>(() => streams.RequestBody.ReadAsync(new byte[1], 0, 1)));

            Assert.Same(ex,
                await Assert.ThrowsAsync<Exception>(() => upgrade.ReadAsync(new byte[1], 0, 1)));

            await upgrade.WriteAsync(new byte[1], 0, 1);
        }

        [Fact]
        public async Task UpgradeAfterAbort()
        {
            var streams = new Streams(Mock.Of<IFrameControl>());

            streams.Start(null);
            var ex = new Exception("My error");
            streams.Abort(ex);

            var upgrade = streams.Upgrade();

            var writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => streams.ResponseBody.WriteAsync(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, writeEx.Message);

            Assert.Same(ex,
              await Assert.ThrowsAsync<Exception>(() => streams.RequestBody.ReadAsync(new byte[1], 0, 1)));

            Assert.Same(ex,
                await Assert.ThrowsAsync<Exception>(() => upgrade.ReadAsync(new byte[1], 0, 1)));

            await upgrade.WriteAsync(new byte[1], 0, 1);
        }
    }
}
