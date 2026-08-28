using Keepwise.Application.Documents;
using Keepwise.Application.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keepwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/attachments")]
public sealed class AttachmentsController(DocumentService documents) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var (stream, fileName, contentType) = await documents.DownloadAsync(id, cancellationToken);
        return File(stream, contentType, fileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await documents.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
