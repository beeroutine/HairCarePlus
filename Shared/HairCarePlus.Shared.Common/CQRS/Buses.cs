using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace HairCarePlus.Shared.Common.CQRS
{
    public interface ICommandBus
    {
        Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
        Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    }

    public interface IQueryBus
    {
        Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    }

    internal sealed class InMemoryCommandBus : ICommandBus
    {
        private readonly IServiceProvider _sp;
        public InMemoryCommandBus(IServiceProvider sp) => _sp = sp;

        public Task SendAsync(ICommand command, CancellationToken token = default)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            dynamic handler = _sp.GetRequiredService(handlerType);
            return handler.HandleAsync((dynamic)command, token);
        }

        public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken token = default)
        {
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
            dynamic handler = _sp.GetRequiredService(handlerType);
            return handler.HandleAsync((dynamic)command, token);
        }
    }

    internal sealed class InMemoryQueryBus : IQueryBus
    {
        private readonly IServiceProvider _sp;
        public InMemoryQueryBus(IServiceProvider sp) => _sp = sp;

        public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken token = default)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
            dynamic handler = _sp.GetRequiredService(handlerType);
            return handler.HandleAsync((dynamic)query, token);
        }
    }

    public static class CqrsServiceCollectionExtensions
    {
        public static IServiceCollection AddCqrs(this IServiceCollection services)
        {
            services.AddSingleton<ICommandBus, InMemoryCommandBus>();
            services.AddSingleton<IQueryBus, InMemoryQueryBus>();
            return services;
        }
    }
} 