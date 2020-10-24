using System.Threading.Tasks;
using Convey.HTTP;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Trill.Services.Stories.Infrastructure.Logging
{
    internal class LogContextMiddleware : IMiddleware
    {
        private readonly ICorrelationIdFactory _correlationIdFactory;
        private readonly ILogger<LogContextMiddleware> _logger;

        public LogContextMiddleware(ICorrelationIdFactory correlationIdFactory,
            ILogger<LogContextMiddleware> logger)
        {
            _correlationIdFactory = correlationIdFactory;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var correlationId = _correlationIdFactory.Create();
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogInformation("Executing log context middleware.");
                await next(context);
            }
        }
    }
}