using Keepwise.Application.Abstractions;
using Keepwise.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Identity;

public sealed class UserService(IKeepwiseDbContext db, ICurrentUser currentUser)
{
    public async Task<User> ProvisionAsync(string firebaseUid, string email, string? displayName, CancellationToken cancellationToken)
    {
        var existing = await db.Users.FirstOrDefaultAsync(
            u => u.FirebaseUid == firebaseUid && u.DeletedAtUtc == null,
            cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                existing.Email = email;
                existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var user = new User
        {
            FirebaseUid = firebaseUid,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName.Trim()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        user.DisplayName = request.DisplayName.Trim();
        user.MobileNumber = string.IsNullOrWhiteSpace(request.MobileNumber) ? null : request.MobileNumber.Trim();
        user.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        user.TimeZoneId = request.TimeZoneId.Trim();
        user.Language = request.Language.Trim();
        user.PushEnabled = request.PushEnabled;
        user.EmailEnabled = request.EmailEnabled;
        user.SmsEnabled = request.SmsEnabled;
        user.WhatsAppEnabled = request.WhatsAppEnabled;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        var existing = await db.UserDevices.FirstOrDefaultAsync(
            d => d.UserId == user.Id && d.PushToken == request.PushToken,
            cancellationToken);

        if (existing is null)
        {
            db.UserDevices.Add(new UserDevice
            {
                UserId = user.Id,
                PushToken = request.PushToken,
                Platform = request.Platform
            });
        }
        else
        {
            existing.LastSeenAtUtc = DateTimeOffset.UtcNow;
            existing.Platform = request.Platform;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static UserProfileDto Map(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.MobileNumber,
        user.CountryCode,
        user.TimeZoneId,
        user.Language,
        user.PushEnabled,
        user.EmailEnabled,
        user.SmsEnabled,
        user.WhatsAppEnabled);

    private async Task<User> RequireUserAsync(CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Id == currentUser.UserId && u.DeletedAtUtc == null,
            cancellationToken);
        return user ?? throw new Common.NotFoundException("User was not found.");
    }
}
