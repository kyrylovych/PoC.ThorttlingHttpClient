using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PoC.HttpRequest.Throttling
{
    public class Program
    {
        static void Main(string[] args)
        {
            var configuration =
                new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

            var sc = new ServiceCollection();

            sc.AddOptions();

            sc.AddSingleton<ConfigurableThrottleProvider>();
            
            sc.AddHttpClient<ExampleHttpClient>("WithoutThrottling");
            sc.AddHttpClient<ExampleHttpClient>()
                .AddThrottling(configuration.GetSection("Throttling"));
            sc.AddHttpClient<ExampleHttpClient>("CustomExampleHttpClient")
                .AddThrottling(configuration.GetSection("Throttling"));

            sc.BuildServiceProvider()
        }
    }
}
