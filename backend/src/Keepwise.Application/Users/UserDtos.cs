namespace Keepwise.Application.Users;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? MobileNumber,
    string CountryCode,
    string TimeZoneId,
    string Language,
    bool PushEnabled,
    bool EmailEnabled,
    bool SmsEnabled,
    bool WhatsAppEnabled);

public sealed record UpdateProfileRequest(
    string DisplayName,
    string? MobileNumber,
    string CountryCode,
    string TimeZoneId,
    string Language,
    bool PushEnabled,
    bool EmailEnabled,
    bool SmsEnabled,
    bool WhatsAppEnabled);

public sealed record DevLoginRequest(string Email, string? DisplayName);

public sealed record AuthResponse(string AccessToken, UserProfileDto User);

public sealed record RegisterDeviceRequest(string PushToken, string Platform);
