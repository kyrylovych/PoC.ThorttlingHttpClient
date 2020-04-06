using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace PoC.HttpRequest.Throttling
{
    public class ThrottleMessageHandler : DelegatingHandler
    {
        private readonly ConfigurableThrottleProvider _configurableThrottleProvider;
        private readonly string _name;

        public ThrottleMessageHandler(ConfigurableThrottleProvider configurableThrottleProvider, string name)
        {
            _configurableThrottleProvider = configurableThrottleProvider;
            _name = name;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var throttler = _configurableThrottleProvider.Get(_name);
            try
            {
                await throttler.WaitAsync(cancellationToken);
                return await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                throttler.Release();
            }
        }
    }
}
