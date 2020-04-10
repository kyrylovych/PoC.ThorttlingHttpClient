using System;
using System.Threading;
using System.Threading.Tasks;
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
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

            var sc = new ServiceCollection();

            sc.AddOptions();


            sc.AddSingleton<ConfigurableThrottleProvider>();

            sc.AddHttpClient<ExampleHttpClient>("WithoutThrottling");
            sc.AddHttpClient<ExampleHttpClient>()
                .AddThrottling(configuration.GetSection("Throttling"));
            sc.AddHttpClient<ExampleHttpClient>("CustomExampleHttpClient")
                .AddThrottling(configuration.GetSection("Throttling"));

            var provider = sc.BuildServiceProvider();


            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine("Trying enter to resource.");
                        var result =
                            provider.GetRequiredService<ConfigurableThrottleProvider>()
                                .Get("ExampleHttpClient");

                        for (var i = 0; i < 6; i++)
                        {
                            Console.WriteLine($"Item {i}");
                            await result.WaitAsync(CancellationToken.None);
                        }

                        for (var i = 0; i < 6; i++)
                            result.Release();

                        Console.WriteLine("Found resource.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                }
            });

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
