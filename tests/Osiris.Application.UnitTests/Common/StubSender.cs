using MediatR;

namespace Osiris.Application.UnitTests.Common;

/// <summary>
/// Minimal <see cref="ISender"/> test double that returns a single preconfigured response.
/// Used to exercise handlers that compose an existing query through MediatR without a mock library.
/// </summary>
internal sealed class StubSender : ISender
{
    private readonly object? _response;

    public StubSender(object? response)
    {
        _response = response;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => Task.FromResult((TResponse)_response!);

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
        => Task.CompletedTask;

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        => Task.FromResult(_response);

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
