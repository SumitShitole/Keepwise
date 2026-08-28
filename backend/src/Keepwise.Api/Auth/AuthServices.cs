using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Keepwise.Application.Identity;
using Keepwise.Application.Users;
using Keepwise.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Keepwise.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public bool AllowDevLogin { get; set; }
    public string DevIssuer { get; set; } = "keepwise";
    public string DevAudience { get; set; } = "keepwise-app";
    public string DevSigningKey { get; set; } = "dev-only-change-me-please-use-32chars!!";
    public string? FirebaseProjectId { get; set; }
}

public sealed class JwtTokenService(IOptions<AuthOptions> options)
{
    public string CreateDevToken(User user)
    {
        var opts = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.DevSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("firebase_uid", user.FirebaseUid)
        };

        var token = new JwtSecurityToken(
            opts.DevIssuer,
            opts.DevAudience,
            claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class HttpCurrentUser : Application.Abstractions.ICurrentUser
{
    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        var principal = accessor.HttpContext?.User;
        IsAuthenticated = principal?.Identity?.IsAuthenticated == true;
        Email = principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal?.FindFirstValue(ClaimTypes.Email)
            ?? string.Empty;
        var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        UserId = Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }

    public Guid UserId { get; }
    public string Email { get; }
    public bool IsAuthenticated { get; }
}
