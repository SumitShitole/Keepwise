using System.Text.Json;
using Keepwise.Domain;

namespace Keepwise.Application.Ingestion;

/// <summary>Accepts only JSON objects that match the extraction schema. Garbage or prompt-injection text is rejected.</summary>
public static class LlmExtractionParser
{
    public static ExtractedPurchase? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var root = document.RootElement;
            var product = ReadString(root, "productName");
            var vendor = ReadString(root, "vendor");
            var amount = ReadDecimal(root, "amount");
            var date = ReadDate(root, "purchaseDate");
            if (product is null && vendor is null && amount is null)
            {
                return null;
            }

            return new ExtractedPurchase(
                true,
                vendor,
                product,
                ReadString(root, "brand"),
                ReadString(root, "model"),
                date,
                amount,
                ReadString(root, "currency") ?? "INR",
                ReadString(root, "orderNumber"),
                ReadString(root, "invoiceNumber"),
                ReadInt(root, "warrantyDurationMonths"),
                ReadDate(root, "warrantyEndDate"),
                ReadString(root, "serialNumber"),
                ReadString(root, "gstin"),
                ReadString(root, "upiReference"),
                ReadInt(root, "returnWindowDays"),
                FieldProvenance.AiInferred,
                0.55,
                new Dictionary<string, double>());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal? ReadDecimal(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetDecimal(out var number) ? number : null;

    private static int? ReadInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : null;

    private static DateOnly? ReadDate(JsonElement root, string name)
    {
        var text = ReadString(root, name);
        return DateOnly.TryParse(text, out var date) ? date : null;
    }
}
