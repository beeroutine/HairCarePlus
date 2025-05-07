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
            var handler = _sp.GetRequiredService(handlerType);
            var method = handlerType.GetMethod("HandleAsync", new[] { command.GetType(), typeof(CancellationToken) });
            if (method == null)
                throw new InvalidOperationException($"HandleAsync not found for {handlerType.FullName}");
            return (Task)method.Invoke(handler, new object[] { command, token });
        }

        public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken token = default)
        {
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
            var handler = _sp.GetRequiredService(handlerType);
            var method = handlerType.GetMethod("HandleAsync", new[] { command.GetType(), typeof(CancellationToken) });
            if (method == null)
                throw new InvalidOperationException($"HandleAsync not found for {handlerType.FullName}");
            return (Task<TResult>)method.Invoke(handler, new object[] { command, token });
        }
    }

    internal sealed class InMemoryQueryBus : IQueryBus
    {
        private readonly IServiceProvider _sp;
        public InMemoryQueryBus(IServiceProvider sp) => _sp = sp;

        public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken token = default)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
            var handler = _sp.GetRequiredService(handlerType);
            var method = handlerType.GetMethod("HandleAsync", new[] { query.GetType(), typeof(CancellationToken) });
            if (method == null)
                throw new InvalidOperationException($"HandleAsync not found for {handlerType.FullName}");
            return (Task<TResult>)method.Invoke(handler, new object[] { query, token });
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