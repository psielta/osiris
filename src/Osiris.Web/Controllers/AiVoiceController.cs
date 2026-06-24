using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.AiAssistant.Services;

namespace Osiris.Web.Controllers;

/// <summary>
/// Cookie-authenticated realtime voice endpoint. The browser opens a WebSocket to <c>/assistant/voice</c>;
/// the shared <see cref="AiVoiceRelay"/> proxies it to a Gemini Live session (key stays server-side).
/// Gated by AiAssistant + AiAssistantVoice (404 when off).
/// </summary>
[Authorize]
[Route("assistant")]
public sealed class AiVoiceController : ControllerBase
{
    private readonly AiFeatureOptions _features;
    private readonly AiAgentOptions _agentOptions;
    private readonly AiVoiceRelay _relay;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAntiforgery _antiforgery;

    public AiVoiceController(
        IOptions<AiFeatureOptions> features,
        IOptions<AiAgentOptions> agentOptions,
        AiVoiceRelay relay,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IAntiforgery antiforgery)
    {
        _features = features.Value;
        _agentOptions = agentOptions.Value;
        _relay = relay;
        _currentUser = currentUser;
        _clock = clock;
        _antiforgery = antiforgery;
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
            return BadRequest("Esperado um WebSocket.");
        }

        if (!IsAllowedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!await ValidateVoiceNonceAsync())
        {
            return BadRequest("Token de segurança inválido.");
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

    private bool IsAllowedOrigin()
    {
        var rawOrigin = Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(rawOrigin))
        {
            return true;
        }

        if (!Uri.TryCreate(rawOrigin, UriKind.Absolute, out var origin))
        {
            return false;
        }

        if (string.Equals(origin.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _agentOptions.VoiceAllowedOrigins
            .Where(allowed => !string.IsNullOrWhiteSpace(allowed))
            .Any(allowed => string.Equals(allowed.TrimEnd('/'), rawOrigin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> ValidateVoiceNonceAsync()
    {
        var token = Request.Query["voiceCsrf"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        Request.Headers["RequestVerificationToken"] = token;
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}
