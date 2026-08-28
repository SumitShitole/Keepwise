using Keepwise.Application.Ingestion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keepwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/ingestion")]
public sealed class IngestionController(IngestionService ingestion) : ControllerBase
{
    [HttpPost("text")]
    public async Task<ActionResult<IngestAcceptedDto>> Text([FromBody] TextIngestRequest request, CancellationToken cancellationToken)
    {
        var source = request.SourceType ?? IngestionSourceType.SharedText;
        var result = await ingestion.IngestTextAsync(request.Text, source, cancellationToken);
        return Accepted($"/v1/purchase-candidates/{result.CandidateId}", result);
    }

    [HttpPost("documents")]
    [RequestSizeLimit(Keepwise.Application.Documents.DocumentService.MaxFileBytes)]
    public async Task<ActionResult<IngestAcceptedDto>> Document(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await ingestion.IngestDocumentAsync(file.FileName, file.ContentType, stream, file.Length, cancellationToken);
        return Accepted($"/v1/purchase-candidates/{result.CandidateId}", result);
    }
}

public sealed record TextIngestRequest(string Text, IngestionSourceType? SourceType);

[ApiController]
[Authorize]
[Route("v1/purchase-candidates")]
public sealed class PurchaseCandidatesController(CandidateService candidates) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseCandidateDto>>> List([FromQuery] CandidateStatus? status, CancellationToken cancellationToken) =>
        Ok(await candidates.ListAsync(status, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseCandidateDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await candidates.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseCandidateDto>> Edit(Guid id, CandidatePayload payload, CancellationToken cancellationToken) =>
        Ok(await candidates.EditAsync(id, payload, cancellationToken));

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var itemId = await candidates.ConfirmAsync(id, cancellationToken);
        return Ok(new { itemId });
    }

    [HttpPost("{id:guid}/ignore")]
    public async Task<IActionResult> Ignore(Guid id, CancellationToken cancellationToken)
    {
        await candidates.IgnoreAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("v1")]
public sealed class PrivacyController(PrivacyService privacy) : ControllerBase
{
    [HttpGet("users/me/ingestion-settings")]
    public async Task<ActionResult<IngestionSettingsDto>> Settings(CancellationToken cancellationToken) =>
        Ok(await privacy.GetSettingsAsync(cancellationToken));

    [HttpPut("users/me/ingestion-settings")]
    public async Task<ActionResult<IngestionSettingsDto>> UpdateSettings(IngestionSettingsDto request, CancellationToken cancellationToken) =>
        Ok(await privacy.UpdateSettingsAsync(request, cancellationToken));

    [HttpGet("privacy")]
    public async Task<ActionResult<PrivacySummaryDto>> Privacy(CancellationToken cancellationToken) =>
        Ok(await privacy.SummaryAsync(cancellationToken));
}
