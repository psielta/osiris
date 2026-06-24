using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.AiAssistant.Services;

namespace Osiris.Api.Controllers.V1;

/// <summary>
/// JWT-authenticated realtime voice endpoint for the mobile app. The client opens a WebSocket to
/// <c>/api/v1/ai/voice</c> (Bearer token on the handshake) and the shared <see cref="AiVoiceRelay"/>
/// proxies it to a Gemini Live session server-side. Gated by AiAssistant + AiAssistantVoice (404 when off).
/// </summary>
[Authorize]
[Route("api/v1/ai")]
public sealed class AiVoiceController : ControllerBase
{
    private readonly AiFeatureOptions _features;
    private readonly AiAgentOptions _agentOptions;
    private readonly AiVoiceRelay _relay;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AiVoiceController(
        IOptions<AiFeatureOptions> features,
        IOptions<AiAgentOptions> agentOptions,
        AiVoiceRelay relay,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _features = features.Value;
        _agentOptions = agentOptions.Value;
        _relay = relay;
        _currentUser = currentUser;
        _clock = clock;
    }

    [HttpGet("voice")]
    public async Task<IActionResult> Voice()
    {
        if (!_features.AiAssistant || !_features.AiAssistantVoice)
        {
            return NotFound();
        }

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            return BadRequest("Expected a WebSocket request.");
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var request = new AiVoiceRelayRequest(
            _currentUser.TenantId,
            _currentUser.UserId ?? string.Empty,
            DateOnly.FromDateTime(_clock.UtcNow),
            ReadConversationId(),
            _features.AiAssistantWrites && _agentOptions.VoiceWritesEnabled,
            Guid.NewGuid().ToString("n"));

        await _relay.RunAsync(socket, request, HttpContext.RequestAborted);
        return new EmptyResult();
    }

    private Guid? ReadConversationId()
    {
        var raw = Request.Query["conversationId"].FirstOrDefault();
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
