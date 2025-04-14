using EnweVolume.Core.Enums;

namespace EnweVolume.Core.Models;

public record Error(ErrorCode Code, ErrorType ErrorType, string? DebugDescription = null);