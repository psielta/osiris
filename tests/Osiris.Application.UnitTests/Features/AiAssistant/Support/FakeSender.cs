using MediatR;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

/// <summary>Minimal <see cref="ISender"/> that returns a single canned response and captures the request.</summary>
internal sealed class FakeSender : ISender
{
    private readonly object _response;

    public FakeSender(object response)
    {
        _response = response;
    }

    public object? LastRequest { get; private set; }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult((TResponse)_response);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        LastRequest = request;
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult<object?>(_response);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
