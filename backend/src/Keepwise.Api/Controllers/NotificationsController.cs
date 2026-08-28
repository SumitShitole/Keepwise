using Keepwise.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/notifications")]
public sealed class NotificationsController(IKeepwiseDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var rows = await db.ReminderOccurrences
            .AsNoTracking()
            .Where(o => o.UserId == currentUser.UserId)
            .OrderByDescending(o => o.ScheduledAtUtc)
            .Take(50)
            .Select(o => new
            {
                o.Id,
                o.Channel,
                o.Status,
                o.ScheduledLocalDate,
                o.ScheduledAtUtc,
                o.SentAtUtc,
                o.LastError
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }
}
