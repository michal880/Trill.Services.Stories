﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convey;
using Convey.CQRS.Commands;
using Convey.CQRS.Events;
using Convey.CQRS.Queries;
using Convey.Discovery.Consul;
using Convey.Docs.Swagger;
using Convey.HTTP;
using Convey.LoadBalancing.Fabio;
using Convey.MessageBrokers;
using Convey.MessageBrokers.CQRS;
using Convey.MessageBrokers.Outbox;
using Convey.MessageBrokers.Outbox.Mongo;
using Convey.MessageBrokers.RabbitMQ;
using Convey.Metrics.Prometheus;
using Convey.Persistence.MongoDB;
using Convey.Persistence.Redis;
using Convey.Security;
using Convey.Tracing.Jaeger;
using Convey.Tracing.Jaeger.RabbitMQ;
using Convey.Types;
using Convey.WebApi;
using Convey.WebApi.CQRS;
using Convey.WebApi.Security;
using Convey.WebApi.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Trill.Services.Stories.Application;
using Trill.Services.Stories.Application.Clients;
using Trill.Services.Stories.Application.Commands;
using Trill.Services.Stories.Application.Events.External;
using Trill.Services.Stories.Application.Services;
using Trill.Services.Stories.Core.Repositories;
using Trill.Services.Stories.Infrastructure.Clients.HTTP;
using Trill.Services.Stories.Infrastructure.Contexts;
using Trill.Services.Stories.Infrastructure.Decorators;
using Trill.Services.Stories.Infrastructure.Exceptions;
using Trill.Services.Stories.Infrastructure.Logging;
using Trill.Services.Stories.Infrastructure.Metrics;
using Trill.Services.Stories.Infrastructure.Mongo;
using Trill.Services.Stories.Infrastructure.Mongo.Documents;
using Trill.Services.Stories.Infrastructure.Mongo.Repositories;
using Trill.Services.Stories.Infrastructure.Protos;
using Trill.Services.Stories.Infrastructure.Services;

namespace Trill.Services.Stories.Infrastructure
{
    public static class Extensions
    {
        public static IConveyBuilder AddInfrastructure(this IConveyBuilder builder)
        {
            builder.Services
                .AddScoped<LogContextMiddleware>()
                .AddSingleton<RequestTypeMetricsMiddleware>()
                .AddSingleton<IRequestStorage, RequestStorage>()
                .AddSingleton<IStoryRequestStorage, StoryRequestStorage>()
                .AddSingleton<IStoryIdGenerator, StoryIdGenerator>()
                .AddSingleton<IDateTimeProvider, DateTimeProvider>()
                .AddScoped<IMessageBroker, MessageBroker>()
                .AddScoped<IStoryRepository, StoryMongoRepository>()
                .AddScoped<IStoryRatingRepository, StoryRatingMongoRepository>()
                .AddScoped<IUserRepository, UserMongoRepository>()
                .AddScoped<IUsersApiClient, UsersApiHttpClient>()
                .AddTransient<IAppContextFactory, AppContextFactory>()
                .AddTransient(ctx => ctx.GetRequiredService<IAppContextFactory>().Create())
                .AddGrpc();
            
            builder.Services.TryDecorate(typeof(ICommandHandler<>), typeof(MetricsCommandHandlerDecorator<>));
            
            builder.Services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingCommandHandlerDecorator<>));
            builder.Services.TryDecorate(typeof(IEventHandler<>), typeof(LoggingEventHandlerDecorator<>));
            
            builder.Services.TryDecorate(typeof(ICommandHandler<>), typeof(OutboxCommandHandlerDecorator<>));
            builder.Services.TryDecorate(typeof(IEventHandler<>), typeof(OutboxEventHandlerDecorator<>));
            
             builder
                .AddErrorHandler<ExceptionToResponseMapper>()
                .AddExceptionToMessageMapper<ExceptionToMessageMapper>()
                .AddQueryHandlers()
                .AddInMemoryQueryDispatcher()
                .AddHttpClient()
                .AddConsul()
                .AddFabio()
                .AddRabbitMq(plugins: p => p.AddJaegerRabbitMqPlugin())
                .AddMessageOutbox(o => o.AddMongo())
                .AddMongo()
                .AddRedis()
                .AddPrometheus()
                .AddJaeger()
                .AddMongoRepository<StoryDocument, long>("stories")
                .AddMongoRepository<UserDocument, Guid>("users")
                .AddWebApiSwaggerDocs()
                .AddCertificateAuthentication()
                .AddSecurity();
             
             builder.Services.AddSingleton<ICorrelationIdFactory, CorrelationIdFactory>();

             return builder;
        }

        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
        {
            app.UseMiddleware<LogContextMiddleware>()
                .UseMiddleware<RequestTypeMetricsMiddleware>()
                .UseErrorHandler()
                .UseSwaggerDocs()
                .UseJaeger()
                .UseConvey()
                .UseMongo()
                .UsePublicContracts<ContractAttribute>()
                .UsePrometheus()
                .UseCertificateAuthentication()
                .UseRabbitMq()
                .SubscribeEvent<UserCreated>()
                .SubscribeEvent<UserLocked>()
                .SubscribeEvent<UserUnlocked>()
                .SubscribeCommand<SendStory>()
                .SubscribeCommand<RateStory>();

            app.UseRouting()
                .UseEndpoints(e => e.MapGrpcService<StoryServiceGrpcServer>());

            return app;
        }

        public static Task GetAppName(this HttpContext httpContext)
            => httpContext.Response.WriteAsync(httpContext.RequestServices.GetService<AppOptions>().Name);

        internal static CorrelationContext GetCorrelationContext(this IHttpContextAccessor accessor)
            => accessor.HttpContext?.Request.Headers.TryGetValue("Correlation-Context", out var json) is true
                ? JsonConvert.DeserializeObject<CorrelationContext>(json.FirstOrDefault())
                : null;

        internal static string GetSpanContext(this IMessageProperties messageProperties, string header)
        {
            if (messageProperties is null)
            {
                return string.Empty;
            }

            if (messageProperties.Headers.TryGetValue(header, out var span) && span is byte[] spanBytes)
            {
                return Encoding.UTF8.GetString(spanBytes);
            }

            return string.Empty;
        }
    }
}