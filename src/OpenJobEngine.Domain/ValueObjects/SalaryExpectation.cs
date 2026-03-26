namespace OpenJobEngine.Domain.ValueObjects;

public sealed class SalaryExpectation
{
    public decimal? MinAmount { get; private set; }

    public decimal? MaxAmount { get; private set; }

    public string? Currency { get; private set; }

    public static SalaryExpectation Empty() => new();

    public void Update(decimal? minAmount, decimal? maxAmount, string? currency)
    {
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        Currency = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant();
    }
}
