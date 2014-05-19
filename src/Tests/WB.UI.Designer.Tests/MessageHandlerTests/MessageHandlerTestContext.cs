﻿using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WB.UI.Designer.Code.MessageHandlers;

namespace WB.UI.Designer.Tests.MessageHandlerTests
{
    public class MessageHandlerTestContext
    {
        public static FakeInnerHandler CreateFakeInnerHandler()
        {
            return new FakeInnerHandler();
        }

        public static HttpsVerifier CreateHttpsVerifier(DelegatingHandler innerhandler)
        {
            return new HttpsVerifier() { InnerHandler = innerhandler };
        }
    }

    public class FakeInnerHandler : DelegatingHandler
    {
        public HttpResponseMessage Message { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Message == null)
            {
                return base.SendAsync(request, cancellationToken);
            }
            return Task.Factory.StartNew(() => Message);
        }
    }

}
