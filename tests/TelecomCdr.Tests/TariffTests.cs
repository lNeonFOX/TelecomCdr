namespace TelecomCdr.Tests;

public class TariffTests
{
    public static TheoryData<string, bool, double, decimal> Cases => new()
    {
        { "KZ", false, 4.0, 60.00m },
        { "KZ", true, 0.5, 50.00m },
        { "US", true, 10.0, 1200.00m },
        { "DE", false, 3.0, 135.00m },
        { "XX", false, 2.0, 90.00m },
        { "KZ", true, 1.0, 45.00m },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Tariff_is_correct(string country, bool roaming, double minutes, decimal expected)
    {
        var record = new CallRecord("R-1", country, minutes, roaming);
        Assert.Equal(expected, CallPricing.CalculateCost(in record));
    }

    [Fact]
    public void Rounds_away_from_zero()
    {
        var record = new CallRecord("R-2", "DE", 0.333, false);   // 45 * 0.333 = 14.985
        Assert.Equal(14.99m, CallPricing.CalculateCost(in record));
    }
}