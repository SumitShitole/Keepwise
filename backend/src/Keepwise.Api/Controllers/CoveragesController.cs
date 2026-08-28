using Keepwise.Application.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keepwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/coverages")]
public sealed class CoveragesController(CoverageService coverages) : ControllerBase
{
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CoverageDto>> Update(Guid id, UpdateCoverageRequest request, CancellationToken cancellationToken) =>
        Ok(await coverages.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/extend")]
    public async Task<ActionResult<CoverageDto>> Extend(Guid id, [FromBody] ExtendRequest request, CancellationToken cancellationToken) =>
        Ok(await coverages.ExtendWarrantyAsync(id, request.DurationValue, request.Unit, cancellationToken));

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, MaintenanceActionRequest request, CancellationToken cancellationToken)
    {
        await coverages.CompleteMaintenanceAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/skip")]
    public async Task<IActionResult> Skip(Guid id, MaintenanceActionRequest request, CancellationToken cancellationToken)
    {
        await coverages.SkipMaintenanceAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, MaintenanceActionRequest request, CancellationToken cancellationToken)
    {
        await coverages.RescheduleMaintenanceAsync(id, request, cancellationToken);
        return NoContent();
    }
}

public sealed record ExtendRequest(int DurationValue, DurationUnit Unit);
