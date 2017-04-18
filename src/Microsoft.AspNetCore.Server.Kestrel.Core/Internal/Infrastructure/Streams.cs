// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    class Streams
    {
        private FrameRequestStream _upgradedRequest;
        private FrameResponseStream _upgradedResponse;

        public Streams(IFrameControl frameControl)
        {
            RequestBody = new FrameRequestStream();
            ResponseBody = new FrameResponseStream(frameControl);
        }

        public FrameRequestStream RequestBody { get; }
        public FrameResponseStream ResponseBody { get; }

        public Stream Upgrade()
        {
            _upgradedRequest = _upgradedRequest ?? RequestBody.Upgrade();
            _upgradedResponse = _upgradedResponse ?? ResponseBody.Upgrade();
            return new FrameDuplexStream(_upgradedRequest, _upgradedResponse);
        }

        public void Start(MessageBody messageBody)
        {
            RequestBody.StartAcceptingReads(messageBody);
            ResponseBody.StartAcceptingWrites();

            _upgradedRequest?.StartAcceptingReads(messageBody);
            _upgradedResponse?.StartAcceptingWrites();
        }

        public void Pause()
        {
            RequestBody.PauseAcceptingReads();
            ResponseBody.PauseAcceptingWrites();

            _upgradedRequest?.PauseAcceptingReads();
            _upgradedResponse?.PauseAcceptingWrites();
        }

        public void Resume()
        {
            RequestBody.ResumeAcceptingReads();
            ResponseBody.ResumeAcceptingWrites();

            _upgradedRequest?.ResumeAcceptingReads();
            _upgradedResponse?.ResumeAcceptingWrites();
        }

        public void Stop()
        {
            RequestBody.StopAcceptingReads();
            ResponseBody.StopAcceptingWrites();

            _upgradedRequest?.StopAcceptingReads();
            _upgradedResponse?.StopAcceptingWrites();
        }

        public void Abort(Exception error)
        {
            RequestBody.Abort(error);
            ResponseBody.Abort();

            _upgradedRequest?.Abort(error);
            _upgradedResponse?.Abort();
        }
    }
}
