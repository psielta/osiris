namespace Osiris.Domain.Enums;

/// <summary>
/// Author of a persisted conversation message. Tool messages carry the redacted result that was
/// returned to the model; raw chain-of-thought is never persisted.
/// </summary>
public enum AiMessageRole
{
    User = 1,
    Assistant = 2,
    Tool = 3
}
