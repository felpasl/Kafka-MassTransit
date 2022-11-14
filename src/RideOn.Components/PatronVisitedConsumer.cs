using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using RideOn.Contracts;

namespace RideOn.Components
{
    public class PatronVisitedConsumer :
        IConsumer<PatronVisited>
    {
        readonly ILogger<PatronVisitedConsumer> _logger;

        public PatronVisitedConsumer(ILogger<PatronVisitedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<PatronVisited> context)
        {
            _logger.LogInformation("Consume Patron Visited: ID:{PatronId} Entered:[{Entered}] Left:[{Left}] Duration:[{Duration}]", context.Message.PatronId,
                context.Message.Entered, context.Message.Left, context.Message.Left - context.Message.Entered);

            return Task.CompletedTask;
        }
    }
}