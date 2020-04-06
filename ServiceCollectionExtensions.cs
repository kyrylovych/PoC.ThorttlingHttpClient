using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace PoC.HttpRequest.Throttling
{
    public static class ServiceCollectionExtensions
    {
        public static IHttpClientBuilder AddThrottling(this IHttpClientBuilder builder, IConfiguration configuration)
        {
            builder.Services.Configure<ThrottleOptions>(builder.Name, configuration);
            builder.AddHttpMessageHandler(p => new ThrottleMessageHandler(p.GetRequiredService<ConfigurableThrottleProvider>(), builder.Name));
            return builder;
        }
    }
}
