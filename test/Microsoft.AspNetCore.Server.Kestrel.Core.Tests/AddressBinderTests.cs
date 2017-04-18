// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class AddressBinderTests
    {
        [Theory]
        [InlineData("10.10.10.10", "10.10.10.10")]
        [InlineData("[::1]", "::1")]
        public void CorrectIPEndpointsAreCreated(string host, string expectedAddress)
        {
            Assert.True(AddressBinder.TryCreateIPEndPoint(
                ServerAddress.FromUrl($"http://{host}:5000/"), out var endpoint));
            Assert.NotNull(endpoint);
            Assert.Equal(IPAddress.Parse(expectedAddress), endpoint.Address);
            Assert.Equal(5000, endpoint.Port);
        }

        [Theory]
        [InlineData("*")]
        [InlineData("randomhost")]
        [InlineData("+")]
        [InlineData("contoso.com")]
        public void DoesNotCreateIPEndpoints(string host)
        {
            Assert.False(AddressBinder.TryCreateIPEndPoint(
                ServerAddress.FromUrl($"http://{host}:5000/"), out var endpoint));
        }

        [Fact]
        public async Task WrapsAddressInUseExceptionAsIOException()
        {
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add("http://localhost:5000");
            var options = new List<ListenOptions>();

            await Assert.ThrowsAsync<IOException>(() =>
                AddressBinder.BindAsync(addresses,
                options,
                NullLogger.Instance,
                endpoint => throw new AddressInUseException("already in use")));
        }

        [Theory]
        [InlineData("http://*:80")]
        [InlineData("http://+:80")]
        [InlineData("http://contoso.com:80")]
        public async Task FallbackToIpV4Any(string address)
        {
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add(address);
            var options = new List<ListenOptions>();

            var ipV6Attempt = false;
            var ipV4Attempt = false;

            await AddressBinder.BindAsync(addresses,
                options,
                NullLogger.Instance,
                endpoint =>
                {
                    if (endpoint.IPEndPoint.Address == IPAddress.IPv6Any)
                    {
                        ipV6Attempt = true;
                        throw new InvalidOperationException("EAFNOSUPPORT");
                    }

                    if (endpoint.IPEndPoint.Address == IPAddress.Any)
                    {
                        ipV4Attempt = true;
                    }

                    return Task.CompletedTask;
                });

            Assert.True(ipV4Attempt, "Should have attempted to bind to IPAddress.Any");
            Assert.True(ipV6Attempt, "Should have attempted to bind to IPAddress.IPv6Any");
        }
    }
}
