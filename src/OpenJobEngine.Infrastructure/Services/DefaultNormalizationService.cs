using System.Globalization;
using System.Text.RegularExpressions;
using OpenJobEngine.Application.Abstractions.Services;
using OpenJobEngine.Domain.Entities;
using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Infrastructure.Services;

public sealed partial class DefaultNormalizationService : INormalizationService
{
    private static readonly Regex NumberRegex = SalaryNumberRegex();
    private static readonly Regex KMultiplierRegex = KMultiplierPattern();

    public JobOffer Normalize(RawJobOffer raw)
    {
        var title = TextCanonicalizer.Clean(raw.Title);
        var company = TextCanonicalizer.Clean(raw.CompanyName);
        var description = string.IsNullOrWhiteSpace(raw.Description) ? null : TextCanonicalizer.Clean(raw.Description);
        var location = string.IsNullOrWhiteSpace(raw.LocationText) ? null : TextCanonicalizer.Clean(raw.LocationText);
        var salaryText = string.IsNullOrWhiteSpace(raw.SalaryText) ? null : TextCanonicalizer.Clean(raw.SalaryText);
        var employmentType = ResolveEmploymentType(title, description, raw.Metadata);
        var seniorityLevel = ResolveSeniority(title, description, raw.Metadata);
        var workMode = ResolveWorkMode(title, description, location, raw.Metadata);
        var (salaryMin, salaryMax, salaryCurrency) = ParseSalary(salaryText, title, description, raw.Metadata);
        var collectedAtUtc = DateTimeOffset.UtcNow;

        return new JobOffer(
            Guid.NewGuid(),
            title,
            company,
            description,
            location,
            employmentType,
            seniorityLevel,
            workMode,
            null,
            null,
            null,
            salaryText,
            salaryMin,
            salaryMax,
            salaryCurrency,
            workMode == WorkMode.Remote,
            raw.Url.Trim(),
            raw.SourceName.Trim(),
            raw.SourceJobId.Trim(),
            raw.PublishedAtUtc,
            collectedAtUtc,
            collectedAtUtc,
            string.Empty,
            true,
            0m,
            null);
    }

    private static EmploymentType ResolveEmploymentType(
        string title,
        string? description,
        IReadOnlyDictionary<string, string> metadata)
    {
        var corpus = string.Join(" ", new[]
        {
            title,
            description,
            metadata.TryGetValue("contract_type", out var contractType) ? contractType : null,
            metadata.TryGetValue("contract_time", out var contractTime) ? contractTime : null
        }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();

        if (corpus.Contains("full time") || corpus.Contains("full-time") || corpus.Contains("tiempo completo"))
        {
            return EmploymentType.FullTime;
        }

        if (corpus.Contains("part time") || corpus.Contains("part-time") || corpus.Contains("medio tiempo"))
        {
            return EmploymentType.PartTime;
        }

        if (corpus.Contains("contract") || corpus.Contains("contrato") || corpus.Contains("freelance") || corpus.Contains("por proyecto"))
        {
            return EmploymentType.Contract;
        }

        if (corpus.Contains("intern") || corpus.Contains("internship") || corpus.Contains("pasant") || corpus.Contains("practicante"))
        {
            return EmploymentType.Internship;
        }

        if (corpus.Contains("temporary") || corpus.Contains("temporal"))
        {
            return EmploymentType.Temporary;
        }

        return EmploymentType.Unknown;
    }

    private static SeniorityLevel ResolveSeniority(string title, string? description, IReadOnlyDictionary<string, string> metadata)
    {
        var corpus = string.Join(" ", new[]
        {
            title,
            description,
            metadata.TryGetValue("category", out var category) ? category : null
        }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();

        if (corpus.Contains("lead") || corpus.Contains("principal") || corpus.Contains("staff engineer"))
        {
            return SeniorityLevel.Lead;
        }

        if (corpus.Contains("director") || corpus.Contains("head of") || corpus.Contains("vp ") || corpus.Contains("chief"))
        {
            return SeniorityLevel.Executive;
        }

        if (corpus.Contains("senior") || corpus.Contains("sr ") || corpus.Contains("sr.") || corpus.Contains("ssr"))
        {
            return SeniorityLevel.Senior;
        }

        if (corpus.Contains("semi senior") || corpus.Contains("semi-senior") || corpus.Contains("mid") || corpus.Contains("middle"))
        {
            return SeniorityLevel.Mid;
        }

        if (corpus.Contains("junior") || corpus.Contains("jr ") || corpus.Contains("jr.") || corpus.Contains("trainee"))
        {
            return SeniorityLevel.Junior;
        }

        return SeniorityLevel.Unknown;
    }

    private static WorkMode ResolveWorkMode(
        string title,
        string? description,
        string? location,
        IReadOnlyDictionary<string, string> metadata)
    {
        var corpus = string.Join(" ", new[]
        {
            title,
            description,
            location,
            metadata.TryGetValue("workplace", out var workplace) ? workplace : null
        }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();

        if (corpus.Contains("hybrid") || corpus.Contains("hibrid"))
        {
            return WorkMode.Hybrid;
        }

        if (corpus.Contains("remote") || corpus.Contains("remoto") || corpus.Contains("work from home") || corpus.Contains("teletrabajo") || corpus.Contains("home office"))
        {
            return WorkMode.Remote;
        }

        if (corpus.Contains("presencial") || corpus.Contains("on-site") || corpus.Contains("onsite"))
        {
            return WorkMode.OnSite;
        }

        return WorkMode.Unknown;
    }

    private static (decimal? SalaryMin, decimal? SalaryMax, string? Currency) ParseSalary(
        string? salaryText,
        string title,
        string? description,
        IReadOnlyDictionary<string, string> metadata)
    {
        var sourceText = string.Join(" ", new[]
        {
            salaryText,
            title,
            description,
            metadata.TryGetValue("salary", out var salaryMetadata) ? salaryMetadata : null
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return (null, null, null);
        }

        var normalized = sourceText.ToLowerInvariant();
        var currency = ResolveCurrency(normalized);
        var hasKMultiplier = KMultiplierRegex.IsMatch(normalized);
        var values = NumberRegex.Matches(normalized)
            .Select(x => ParseDecimal(x.Value))
            .Where(x => x.HasValue)
            .Select(x =>
            {
                var value = x!.Value;
                return hasKMultiplier && value < 1000m ? value * 1000m : value;
            })
            .ToArray();

        return values.Length switch
        {
            0 => (null, null, currency),
            1 => (values[0], values[0], currency),
            _ => (Math.Min(values[0], values[1]), Math.Max(values[0], values[1]), currency)
        };
    }

    private static string? ResolveCurrency(string text)
    {
        if (text.Contains("usd") || text.Contains("us$") || text.Contains("dolar"))
        {
            return "USD";
        }

        if (text.Contains("eur") || text.Contains("€") || text.Contains("euro"))
        {
            return "EUR";
        }

        if (text.Contains("cop") || text.Contains("col$") || text.Contains("colomb") || text.Contains("millones"))
        {
            return "COP";
        }

        if (text.Contains("mxn") || text.Contains("mx$"))
        {
            return "MXN";
        }

        return null;
    }

    private static decimal? ParseDecimal(string value)
    {
        var sanitized = value.Replace(" ", string.Empty);

        if (sanitized.Contains('.') && sanitized.Contains(','))
        {
            sanitized = sanitized.Replace(".", string.Empty).Replace(",", ".");
        }
        else if (sanitized.Count(x => x == '.') > 1)
        {
            sanitized = sanitized.Replace(".", string.Empty);
        }
        else if (sanitized.Count(x => x == ',') > 1)
        {
            sanitized = sanitized.Replace(",", string.Empty);
        }
        else if (sanitized.Contains('.') && LooksLikeThousandsSeparator(sanitized, '.'))
        {
            sanitized = sanitized.Replace(".", string.Empty);
        }
        else if (sanitized.Contains(',') && LooksLikeThousandsSeparator(sanitized, ','))
        {
            sanitized = sanitized.Replace(",", string.Empty);
        }
        else if (sanitized.Contains(','))
        {
            sanitized = sanitized.Replace(",", ".");
        }

        return decimal.TryParse(sanitized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static bool LooksLikeThousandsSeparator(string value, char separator)
    {
        var segments = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 2 && segments[1].Length == 3 && segments[0].Length <= 3;
    }

    [GeneratedRegex(@"(?<!\w)\d{1,3}(?:[.,\s]\d{3})*(?:[.,]\d+)?(?!\w)", RegexOptions.Compiled)]
    private static partial Regex SalaryNumberRegex();

    [GeneratedRegex(@"\b\d+(?:[.,]\d+)?\s*k\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex KMultiplierPattern();
}
