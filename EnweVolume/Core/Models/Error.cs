using EnweVolume.Core.Enums;

namespace EnweVolume.Core.Models;

public sealed record Error(
    ErrorCode Code,
    string? Message = null)
{
    public static Error From(ErrorCode code, string? message = null) =>
        new(code, message);
}