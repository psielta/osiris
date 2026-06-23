using MediatR;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

/// <summary>An <see cref="ISender"/> that routes each request to a handler registered by request type and counts invocations.</summary>
internal sealed class MapSender : ISender
{
    private readonly Dictionary<Type, Func<object, object>> _handlers = new();
    private readonly Dictionary<Type, int> _counts = new();

    public MapSender On<TRequest>(Func<TRequest, object> handler)
    {
        _handlers[typeof(TRequest)] = request => handler((TRequest)request);
        return this;
    }

    public int Invocations<TRequest>() => _counts.GetValueOrDefault(typeof(TRequest));

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var type = request.GetType();
        _counts[type] = _counts.GetValueOrDefault(type) + 1;

        if (_handlers.TryGetValue(type, out var handler))
        {
            return Task.FromResult((TResponse)handler(request));
        }

        throw new InvalidOperationException($"No handler registered for {type.Name}.");
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest =>
        throw new NotSupportedException();

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
