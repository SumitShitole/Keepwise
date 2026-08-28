using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Keepwise.Domain;

public sealed record ExtractedPurchase(
    bool IsPurchase,
    string? Vendor,
    string? ProductName,
    string? Brand,
    string? Model,
    DateOnly? PurchaseDate,
    decimal? Amount,
    string Currency,
    string? OrderNumber,
    string? InvoiceNumber,
    int? WarrantyDurationMonths,
    DateOnly? WarrantyEndDate,
    string? SerialNumber,
    string? Gstin,
    string? UpiReference,
    int? ReturnWindowDays,
    FieldProvenance WarrantyProvenance,
    double OverallConfidence,
    IReadOnlyDictionary<string, double> FieldConfidence);

public static class PurchaseFingerprint
{
    public static string Build(
        Guid userId,
        string? vendor,
        string? orderNumber,
        string? invoiceNumber,
        decimal? amount,
        DateOnly? date,
        string? productName)
    {
        var vendorSlug = Slug(vendor);
        if (!string.IsNullOrWhiteSpace(orderNumber) || !string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return $"{userId:N}|{vendorSlug}|{Slug(orderNumber)}|{Slug(invoiceNumber)}";
        }

        var amountKey = amount is null ? "" : amount.Value.ToString("0.00", CultureInfo.InvariantCulture);
        var dateKey = date?.ToString("yyyy-MM-dd") ?? "";
        return $"{userId:N}|{vendorSlug}|{amountKey}|{dateKey}|{Slug(productName)}";
    }

    public static string Slug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalized = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }
}

public static class IndiaPurchaseNormalizer
{
    private static readonly Regex AmountRegex = new(
        @"(?:₹|rs\.?|inr)\s*([0-9]{1,3}(?:,[0-9]{2,3})*(?:\.[0-9]{1,2})?|[0-9]+(?:\.[0-9]{1,2})?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OrderAmazon = new(@"\b(\d{3}-\d{7}-\d{7})\b", RegexOptions.Compiled);
    private static readonly Regex OrderFlipkart = new(@"\b(OD\d{10,})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Gstin = new(@"\b(\d{2}[A-Z]{5}\d{4}[A-Z][A-Z0-9]Z[A-Z0-9])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Upi = new(@"\b(?:upi(?:\s*ref(?:erence)?)?|utr)[:\s-]*([A-Z0-9]{8,})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Warranty = new(@"\b(?:warranty|guarantee)\s*(?:of|for|:)?\s*(\d+)\s*(year|years|yr|yrs|month|months|mo)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ReturnDays = new(@"\b(?:return|replacement)\s*(?:window|period)?\s*(?:of|for|:)?\s*(\d+)\s*days?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static decimal? TryAmount(string text)
    {
        var match = AmountRegex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var raw = match.Groups[1].Value.Replace(",", "", StringComparison.Ordinal);
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    public static DateOnly? TryDate(string text)
    {
        var formats = new[]
        {
            "d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy", "dd MMMM yyyy",
            "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"
        };
        foreach (Match match in Regex.Matches(text, @"\b(\d{1,2}[ /-][A-Za-z]{3,9}[ /,-]\d{4}|\d{1,2}/\d{1,2}/\d{4}|\d{4}-\d{2}-\d{2})\b"))
        {
            var token = match.Value.Replace(",", " ");
            if (DateOnly.TryParseExact(token, formats, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.None, out var date) ||
                DateOnly.TryParseExact(token, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date;
            }
        }

        return null;
    }

    public static string? TryOrderNumber(string text) =>
        OrderAmazon.Match(text) is { Success: true } amazon ? amazon.Groups[1].Value
        : OrderFlipkart.Match(text) is { Success: true } flipkart ? flipkart.Groups[1].Value.ToUpperInvariant()
        : null;

    public static string? TryGstin(string text) =>
        Gstin.Match(text) is { Success: true } match ? match.Groups[1].Value.ToUpperInvariant() : null;

    public static string? TryUpi(string text) =>
        Upi.Match(text) is { Success: true } match ? match.Groups[1].Value.ToUpperInvariant() : null;

    public static (int? Months, FieldProvenance Provenance) TryWarrantyMonths(string text)
    {
        var match = Warranty.Match(text);
        if (!match.Success)
        {
            return (null, FieldProvenance.Estimated);
        }

        var value = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Value.ToLowerInvariant();
        var months = unit.StartsWith("year", StringComparison.Ordinal) || unit.StartsWith("yr", StringComparison.Ordinal)
            ? value * 12
            : value;
        return (months, FieldProvenance.VendorProvided);
    }

    public static int? TryReturnDays(string text)
    {
        var match = ReturnDays.Match(text);
        return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
    }

    public static string? TryVendor(string text)
    {
        if (text.Contains("amazon", StringComparison.OrdinalIgnoreCase))
        {
            return "Amazon";
        }

        if (text.Contains("flipkart", StringComparison.OrdinalIgnoreCase))
        {
            return "Flipkart";
        }

        if (text.Contains("croma", StringComparison.OrdinalIgnoreCase))
        {
            return "Croma";
        }

        if (text.Contains("reliance digital", StringComparison.OrdinalIgnoreCase))
        {
            return "Reliance Digital";
        }

        return null;
    }
}
