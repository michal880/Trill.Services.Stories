using System.Threading.Tasks;
using Convey.CQRS.Events;
using Convey.HTTP;
using Convey.Types;
using Serilog.Context;

namespace Trill.Services.Stories.Infrastructure.Decorators
{
    [Decorator]
    internal sealed class LoggingEventHandlerDecorator<TEvent> : IEventHandler<TEvent>
        where TEvent : class, IEvent
    {
        private readonly IEventHandler<TEvent> _handler;
        private readonly ICorrelationIdFactory _correlationIdFactory;

        public LoggingEventHandlerDecorator(IEventHandler<TEvent> handler, ICorrelationIdFactory correlationIdFactory)
        {
            _handler = handler;
            _correlationIdFactory = correlationIdFactory;
        }

        public async Task HandleAsync(TEvent @event)
        {
            var correlationId = _correlationIdFactory.Create();
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _handler.HandleAsync(@event);
            }
        }
    }
}