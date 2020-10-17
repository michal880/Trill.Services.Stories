using System.Threading.Tasks;
using Convey.CQRS.Commands;
using Convey.HTTP;
using Convey.Types;
using Serilog.Context;

namespace Trill.Services.Stories.Infrastructure.Decorators
{
    [Decorator]
    internal sealed class LoggingCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        private readonly ICommandHandler<TCommand> _handler;
        private readonly ICorrelationIdFactory _correlationIdFactory;

        public LoggingCommandHandlerDecorator(ICommandHandler<TCommand> handler,
            ICorrelationIdFactory correlationIdFactory)
        {
            _handler = handler;
            _correlationIdFactory = correlationIdFactory;
        }

        public async Task HandleAsync(TCommand command)
        {
            var correlationId = _correlationIdFactory.Create();
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _handler.HandleAsync(command);
            }
        }
    }
}