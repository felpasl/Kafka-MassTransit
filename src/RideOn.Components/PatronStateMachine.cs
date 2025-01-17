﻿using System;
using MassTransit;
using Microsoft.Extensions.Logging;
using RideOn.Contracts;

namespace RideOn.Components
{
    public sealed class PatronStateMachine :
        MassTransitStateMachine<PatronState>
    {
        readonly ILogger<PatronStateMachine> _logger;
        public PatronStateMachine(ILogger<PatronStateMachine> logger)
        {
            _logger = logger;

            Event(() => Entered, x => x.CorrelateById(m => m.Message.PatronId));
            Event(() => Left, x => x.CorrelateById(m => m.Message.PatronId));

            InstanceState(x => x.CurrentState, Tracking);

            Initially(
                When(Entered)
                    .Then(context => _logger.LogInformation("State Initial Entered: Id:{0}", context.Saga.CorrelationId))
                    .Then(context => context.Saga.Entered = context.Message.Timestamp)
                    .TransitionTo(Tracking),
                When(Left)
                    .Then(context => _logger.LogInformation("State Inital Left: Id:{0}", context.Saga.CorrelationId))
                    .Then(context => context.Saga.Left = context.Message.Timestamp)
                    .TransitionTo(Tracking)
            );

            During(Tracking,
                When(Entered)
                    .Then(context => _logger.LogInformation("State Tracking Entered: Id:{0}", context.Saga.CorrelationId))
                    .Then(context => context.Saga.Entered = context.Message.Timestamp),
                When(Left)
                    .Then(context => _logger.LogInformation("State Tracking Left: Id:{0}", context.Saga.CorrelationId))
                    .Then(context => context.Saga.Left = context.Message.Timestamp)
            );

            CompositeEvent(() => Visited, x => x.VisitedStatus, CompositeEventOptions.IncludeInitial, Entered, Left);

            DuringAny(
                When(Visited)
                    .Then(context => _logger.LogInformation("State Visited: Id:{0}", context.Saga.CorrelationId))

                    // Publish will go to RabbitMQ, via the bus
                    //.PublishAsync(context => context.Init<PatronVisited>(new
                    //{
                    //    PatronId = context.Saga.CorrelationId,
                    //    context.Saga.Entered,
                    //    context.Saga.Left
                    //}))

                    // Produce will go to Kafka
                    .Produce(context => context.Init<PatronVisited>(new
                    {
                        PatronId = context.Saga.CorrelationId,
                        context.Saga.Entered,
                        context.Saga.Left
                    }))
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        public State Tracking { get; private set; }
        public Event<PatronEntered> Entered { get; private set; }
        public Event<PatronLeft> Left { get; private set; }
        public Event Visited { get; private set; }
    }
}