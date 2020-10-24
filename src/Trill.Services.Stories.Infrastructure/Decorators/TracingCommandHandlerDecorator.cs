using System.Threading.Tasks;
using Convey;
using Convey.CQRS.Commands;
using OpenTracing;

namespace Trill.Services.Stories.Infrastructure.Decorators
{
    public class TracingCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        private readonly ICommandHandler<TCommand> _handler;
        private readonly ITracer _tracer;

        public TracingCommandHandlerDecorator(ICommandHandler<TCommand> handler, ITracer tracer)
        {
            _handler = handler;
            _tracer = tracer;
        }
        
        public async Task HandleAsync(TCommand command)
        {
            var commandName = typeof(TCommand).Name.Underscore();
            using (var scope = BuildScope(commandName))
            {
                var span = scope.Span;
                span.Log($"Handling a command: {commandName}");
                await _handler.HandleAsync(command);
            }
        }

        private IScope BuildScope(string commandName)
        {
            var scope = _tracer.BuildSpan($"handling-{commandName}");
            if (_tracer.ActiveSpan is {})
            {
                scope.AddReference(References.ChildOf, _tracer.ActiveSpan.Context);
            }

            return scope.StartActive(true);
        }
    }
}