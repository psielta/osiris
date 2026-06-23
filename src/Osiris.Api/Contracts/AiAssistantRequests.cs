namespace Osiris.Api.Contracts;

public sealed record SendAiMessageRequest(string Message);

public sealed record AiFeedbackRequest(int Rating, string? ReasonCode, string? Comment);
