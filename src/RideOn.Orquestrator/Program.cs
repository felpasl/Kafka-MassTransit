using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RideOn.Components;
using RideOn.Contracts;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace RideOn.Orquestrator
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
                            rider.AddSagaStateMachine<PatronStateMachine, PatronState, PatronStateDefinition>()
                                .InMemoryRepository();

                            rider.AddProducer<PatronVisited>(nameof(PatronVisited));

                            rider.UsingKafka((context, k) =>
                            {
                                k.Host("broker:29092");

                                k.TopicEndpoint<Null, PatronEntered>(nameof(PatronEntered), nameof(RideOn), c =>
                                {
                                    c.AutoOffsetReset = AutoOffsetReset.Earliest;
                                    c.CreateIfMissing(t => t.NumPartitions = 1);
                                    c.ConfigureSaga<PatronState>(context);
                                });

                                k.TopicEndpoint<Null, PatronLeft>(nameof(PatronLeft), nameof(RideOn), c =>
                                {
                                    c.AutoOffsetReset = AutoOffsetReset.Earliest;
                                    c.CreateIfMissing(t => t.NumPartitions = 1);
                                    c.ConfigureSaga<PatronState>(context);
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