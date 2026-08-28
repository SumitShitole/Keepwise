using Keepwise.Application.Abstractions;
using Keepwise.Domain;
using UglyToad.PdfPig;

namespace Keepwise.Infrastructure.Ingestion;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public string? Extract(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        var pages = document.GetPages().Select(p => p.Text);
        var text = string.Join('\n', pages).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

public sealed class NoOpOcrProvider : IOcrProvider
{
    public Task<string?> ExtractTextAsync(Stream content, string contentType, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);
}

public sealed class NoOpLlmExtractor : ILlmExtractor
{
    public Task<ExtractedPurchase?> ExtractAsync(string untrustedText, CancellationToken cancellationToken) =>
        Task.FromResult<ExtractedPurchase?>(null);
}
