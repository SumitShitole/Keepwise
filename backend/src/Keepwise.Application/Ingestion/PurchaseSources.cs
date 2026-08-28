using Keepwise.Application.Abstractions;
using Keepwise.Domain;
using Keepwise.Domain.Entities;

namespace Keepwise.Application.Ingestion;

public sealed class DocumentPurchaseSource : IPurchaseSource
{
    public IngestionSourceType SourceType => IngestionSourceType.Document;
    public bool IsEnabled(UserIngestionSettings settings) => settings.ReceiptScanningEnabled;
}

public sealed class SharedTextPurchaseSource : IPurchaseSource
{
    public IngestionSourceType SourceType => IngestionSourceType.SharedText;
    public bool IsEnabled(UserIngestionSettings settings) => settings.SharedTextEnabled;
}
