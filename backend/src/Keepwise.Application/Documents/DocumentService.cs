using Keepwise.Application.Abstractions;
using Keepwise.Application.Common;
using Keepwise.Application.Items;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Documents;

public sealed class DocumentService(IKeepwiseDbContext db, ICurrentUser currentUser, IFileStorage storage, IClock clock)
{
    public static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public const long MaxFileBytes = 10 * 1024 * 1024;

    public async Task<AttachmentDto> UploadAsync(
        Guid itemId,
        string fileName,
        string contentType,
        Stream content,
        long length,
        CancellationToken cancellationToken)
    {
        if (length <= 0 || length > MaxFileBytes)
        {
            throw new AppValidationException("File must be between 1 byte and 10 MB.");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new AppValidationException("Allowed types: PDF, JPEG, PNG, WebP.");
        }

        var item = await db.Items.FirstOrDefaultAsync(
            i => i.Id == itemId && i.UserId == currentUser.UserId && i.DeletedAtUtc == null,
            cancellationToken) ?? throw new NotFoundException("Item was not found.");

        var key = await storage.SaveAsync(content, fileName, contentType, cancellationToken);
        var attachment = new Attachment
        {
            UserId = currentUser.UserId,
            ItemId = item.Id,
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            SizeBytes = length,
            StorageKey = key
        };
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);
        return new AttachmentDto(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedAtUtc);
    }

    public async Task<(Stream Stream, string FileName, string ContentType)> DownloadAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await RequireAsync(attachmentId, cancellationToken);
        var stream = await storage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return (stream, attachment.FileName, attachment.ContentType);
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await RequireAsync(attachmentId, cancellationToken);
        attachment.DeletedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(attachment.StorageKey, cancellationToken);
    }

    private async Task<Attachment> RequireAsync(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.FirstOrDefaultAsync(
            a => a.Id == id && a.UserId == currentUser.UserId && a.DeletedAtUtc == null,
            cancellationToken);
        return attachment ?? throw new NotFoundException("Document was not found.");
    }
}
