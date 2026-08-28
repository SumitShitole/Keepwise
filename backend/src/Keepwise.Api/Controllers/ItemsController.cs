using Keepwise.Application.Common;
using Keepwise.Application.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keepwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/items")]
public sealed class ItemsController(ItemService items, CoverageService coverages, DocumentService documents) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ItemSummaryDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? brand,
        [FromQuery] CoverageStatus? warrantyStatus,
        [FromQuery] bool? archived,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await items.SearchAsync(new ItemListQuery(search, categoryId, brand, warrantyStatus, archived, sort, page, pageSize), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ItemDetailDto>> Create(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var created = await items.CreateAsync(request, cancellationToken);
        return Created($"/v1/items/{created.Id}", created);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemDetailDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await items.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemDetailDto>> Update(Guid id, UpdateItemRequest request, CancellationToken cancellationToken) =>
        Ok(await items.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await items.ArchiveAsync(id, true, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await items.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/coverages")]
    public async Task<ActionResult<CoverageDto>> AddCoverage(Guid id, CreateCoverageRequest request, CancellationToken cancellationToken) =>
        Ok(await coverages.AddAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(DocumentService.MaxFileBytes)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var saved = await documents.UploadAsync(id, file.FileName, file.ContentType, stream, file.Length, cancellationToken);
        return Ok(saved);
    }
}
