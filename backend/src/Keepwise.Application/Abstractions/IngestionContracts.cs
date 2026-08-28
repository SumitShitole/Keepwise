using Keepwise.Domain;
using Keepwise.Domain.Entities;

namespace Keepwise.Application.Abstractions;

/// <summary>User-initiated capture. Phase 1 is HTTP paste/upload; email OAuth is later.</summary>
public interface IPurchaseSource
{
    IngestionSourceType SourceType { get; }
    bool IsEnabled(UserIngestionSettings settings);
}

public interface IOcrProvider
{
    Task<string?> ExtractTextAsync(Stream content, string contentType, CancellationToken cancellationToken);
}

public interface ILlmExtractor
{
    Task<ExtractedPurchase?> ExtractAsync(string untrustedText, CancellationToken cancellationToken);
}

public interface IPdfTextExtractor
{
    string? Extract(byte[] pdfBytes);
}

public interface IHeuristicExtractor
{
    ExtractedPurchase Extract(string text);
}
