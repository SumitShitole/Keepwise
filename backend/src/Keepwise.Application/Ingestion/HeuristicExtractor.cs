using System.Text.RegularExpressions;
using Keepwise.Application.Abstractions;
using Keepwise.Domain;

namespace Keepwise.Application.Ingestion;

public sealed class HeuristicExtractor : IHeuristicExtractor
{
    private static readonly Regex ProductLine = new(
        @"(?im)^(?:item|product|bought|purchased)?\s*[:\-]?\s*(.+)$",
        RegexOptions.Compiled);

    public ExtractedPurchase Extract(string text)
    {
        var vendor = IndiaPurchaseNormalizer.TryVendor(text);
        var amount = IndiaPurchaseNormalizer.TryAmount(text);
        var date = IndiaPurchaseNormalizer.TryDate(text);
        var order = IndiaPurchaseNormalizer.TryOrderNumber(text);
        var gstin = IndiaPurchaseNormalizer.TryGstin(text);
        var upi = IndiaPurchaseNormalizer.TryUpi(text);
        var (warrantyMonths, warrantyProvenance) = IndiaPurchaseNormalizer.TryWarrantyMonths(text);
        var returnDays = IndiaPurchaseNormalizer.TryReturnDays(text);
        var product = TryProductName(text, vendor);
        var brand = TryBrand(product);

        var looksLikePurchase =
            text.Contains("order", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("invoice", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("receipt", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("purchased", StringComparison.OrdinalIgnoreCase) ||
            vendor is not null ||
            amount is not null;

        var confidence = 0.2;
        var fields = new Dictionary<string, double>();
        void Hit(string name, bool ok, double score)
        {
            if (ok)
            {
                confidence += score;
                fields[name] = Math.Min(0.99, score + 0.5);
            }
        }

        Hit("vendor", vendor is not null, 0.15);
        Hit("amount", amount is not null, 0.15);
        Hit("purchaseDate", date is not null, 0.15);
        Hit("orderNumber", order is not null, 0.2);
        Hit("productName", product is not null, 0.15);
        Hit("warranty", warrantyMonths is not null, 0.1);

        return new ExtractedPurchase(
            looksLikePurchase && (product is not null || order is not null || amount is not null),
            vendor,
            product,
            brand,
            null,
            date,
            amount,
            "INR",
            order,
            null,
            warrantyMonths,
            null,
            null,
            gstin,
            upi,
            returnDays,
            warrantyProvenance,
            Math.Min(0.99, confidence),
            fields);
    }

    private static string? TryProductName(string text, string? vendor)
    {
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.Contains("samsung", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("refrigerator", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("washing", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("laptop", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("iphone", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("air conditioner", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("ac ", StringComparison.OrdinalIgnoreCase))
            {
                return line.Length > 120 ? line[..120] : line;
            }
        }

        var match = ProductLine.Match(text);
        if (match.Success)
        {
            var value = match.Groups[1].Value.Trim();
            if (value.Length is > 3 and < 120 && !value.Equals(vendor, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    private static string? TryBrand(string? product)
    {
        if (product is null)
        {
            return null;
        }

        foreach (var brand in new[] { "Samsung", "LG", "Voltas", "Apple", "Sony", "Whirlpool", "Bosch" })
        {
            if (product.Contains(brand, StringComparison.OrdinalIgnoreCase))
            {
                return brand;
            }
        }

        return null;
    }
}
