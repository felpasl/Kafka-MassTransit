using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RideOn.Components;
using RideOn.Contracts;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace RideOn.Consumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            var services = new ServiceCollection();

            services.TryAddSingleton<ILoggerFactory>(new SerilogLoggerFactory());
            services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) => cfg.ConfigureEndpoints(context));

                x.AddRider(rider =>
                {
                    rider.AddConsumer<PatronVisitedConsumer>();

                    rider.UsingKafka((context, k) =>
                    {
                        k.Host("localhost:9092");

                        k.TopicEndpoint<PatronVisited>(nameof(PatronVisited), $"{nameof(RideOn)}-2", c =>
                        {
                            c.AutoOffsetReset = AutoOffsetReset.Earliest;
                            c.CreateIfMissing(t => t.NumPartitions = 1);
                            c.ConfigureConsumer<PatronVisitedConsumer>(context);
                        });
                    });
                });
            });

            await using var provider = services.BuildServiceProvider(true);

            var logger = provider.GetRequiredService<ILogger<Program>>();

            var busControl = provider.GetRequiredService<IBusControl>();

            var startTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

            await busControl.StartAsync(startTokenSource);
            try
            {
                while (true)
                {
                    Console.Write("Consumer-2 Started, anykey to quit: ");
                    var line = Console.ReadLine();

                    if (!string.IsNullOrWhiteSpace(line))
                        break;
                }
            }
            finally
            {
                await busControl.StopAsync(TimeSpan.FromSeconds(30));
            }
        }
    }
}