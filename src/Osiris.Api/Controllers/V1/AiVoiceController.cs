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
    private readonly AiVoiceRelay _relay;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AiVoiceController(
        IOptions<AiFeatureOptions> features,
        AiVoiceRelay relay,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _features = features.Value;
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
        var context = new AiAgentContext(
            _currentUser.TenantId,
            _currentUser.UserId ?? string.Empty,
            Guid.NewGuid(),
            Guid.NewGuid().ToString("n"),
            DateOnly.FromDateTime(_clock.UtcNow),
            WritesEnabled: false);

        await _relay.RunAsync(socket, context, HttpContext.RequestAborted);
        return new EmptyResult();
    }
}
