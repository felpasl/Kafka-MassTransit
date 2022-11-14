using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RideOn.Components;
using RideOn.Contracts;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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


            await Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services.TryAddSingleton<ILoggerFactory>(new SerilogLoggerFactory());
                    services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

                    services.AddMassTransit(x =>
                    {
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host("rabbit");
                            cfg.ConfigureEndpoints(context);
                        });

                        x.AddRider(rider =>
                        {
                            rider.AddConsumer<PatronVisitedConsumer>();

                            rider.UsingKafka((context, k) =>
                            {
                                k.Host("broker:29092");

                                k.TopicEndpoint<PatronVisited>(nameof(PatronVisited), $"{nameof(RideOn)}-1", c =>
                                {
                                    c.AutoOffsetReset = AutoOffsetReset.Earliest;
                                    c.CreateIfMissing(t => t.NumPartitions = 1);
                                    c.ConfigureConsumer<PatronVisitedConsumer>(context);
                                });
                            });
                        });
                    });
                })
                .Build()
                .RunAsync();
        }
    }
}