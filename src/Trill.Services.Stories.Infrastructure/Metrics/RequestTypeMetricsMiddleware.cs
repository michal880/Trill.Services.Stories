using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace Trill.Services.Stories.Infrastructure.Metrics
{
    internal class RequestTypeMetricsMiddleware : IMiddleware
    {
        private readonly Counter _requestsCounter;

        public RequestTypeMetricsMiddleware()
        {
            _requestsCounter = Prometheus.Metrics.CreateCounter("requests_counter", "Number of HTTP requests");
        }
        
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            _requestsCounter.Inc();
            await next(context);
        }
    }
}