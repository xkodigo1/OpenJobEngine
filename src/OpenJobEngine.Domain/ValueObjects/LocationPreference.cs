namespace OpenJobEngine.Domain.ValueObjects;

public sealed class LocationPreference
{
    public string? CurrentCity { get; private set; }

    public string? CurrentRegion { get; private set; }

    public string? CurrentCountryCode { get; private set; }

    public string? TargetCitiesCsv { get; private set; }

    public string? TargetCountriesCsv { get; private set; }

    public string? TargetTimezonesCsv { get; private set; }

    public bool IsWillingToRelocate { get; private set; }

    public static LocationPreference Empty() => new();

    public void Update(
        string? currentCity,
        string? currentRegion,
        string? currentCountryCode,
        IEnumerable<string>? targetCities,
        IEnumerable<string>? targetCountries,
        IEnumerable<string>? targetTimezones,
        bool isWillingToRelocate)
    {
        CurrentCity = NormalizeValue(currentCity);
        CurrentRegion = NormalizeValue(currentRegion);
        CurrentCountryCode = NormalizeCountry(currentCountryCode);
        TargetCitiesCsv = JoinValues(targetCities);
        TargetCountriesCsv = JoinCountries(targetCountries);
        TargetTimezonesCsv = JoinValues(targetTimezones);
        IsWillingToRelocate = isWillingToRelocate;
    }

    public IReadOnlyCollection<string> GetTargetCities()
    {
        return SplitCsv(TargetCitiesCsv);
    }

    public IReadOnlyCollection<string> GetTargetCountries()
    {
        return SplitCsv(TargetCountriesCsv);
    }

    public IReadOnlyCollection<string> GetTargetTimezones()
    {
        return SplitCsv(TargetTimezonesCsv);
    }

    private static string? JoinValues(IEnumerable<string>? values)
    {
        var normalized = values?
            .Select(NormalizeValue)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized is { Length: > 0 } ? string.Join(",", normalized) : null;
    }

    private static string? JoinCountries(IEnumerable<string>? values)
    {
        var normalized = values?
            .Select(NormalizeCountry)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized is { Length: > 0 } ? string.Join(",", normalized) : null;
    }

    private static IReadOnlyCollection<string> SplitCsv(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeCountry(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }
}
