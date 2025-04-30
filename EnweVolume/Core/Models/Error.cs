using EnweVolume.Core.Enums;

namespace EnweVolume.Core.Models;

public record Error(ErrorType ErrorType, ErrorCode Code, string? DebugDescription = null);