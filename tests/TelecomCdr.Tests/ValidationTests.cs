namespace TelecomCdr.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_id(string? id) =>
        Assert.Throws<ArgumentException>(() => { _ = new CallRecord(id!, "KZ", 1.0, false); });

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_country(string? country) =>
        Assert.Throws<ArgumentException>(() => { _ = new CallRecord("R-1", country!, 1.0, false); });

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(10000.01)]
    public void Rejects_bad_duration(double minutes) =>
        Assert.Throws<ArgumentException>(() => { _ = new CallRecord("R-1", "KZ", minutes, false); });

    [Fact]
    public void CalculateCost_rejects_default_record()
    {
        var invalid = default(CallRecord);
        Assert.Throws<ArgumentException>(() => { _ = CallPricing.CalculateCost(in invalid); });
    }

    [Fact]
    public void Zero_minute_calls()
    {
        var roaming = new CallRecord("R-1", "KZ", 0.0, true);
        var normal = new CallRecord("R-2", "KZ", 0.0, false);
        Assert.Equal(50.00m, CallPricing.CalculateCost(in roaming));
        Assert.Equal(0.00m, CallPricing.CalculateCost(in normal));
    }

    [Fact]
    public void Boundary_at_one_minute()
    {
        var below = new CallRecord("R-1", "KZ", Math.BitDecrement(1.0), true);
        var exact = new CallRecord("R-2", "KZ", 1.0, true);
        Assert.Equal(50.00m, CallPricing.CalculateCost(in below));
        Assert.Equal(45.00m, CallPricing.CalculateCost(in exact));
    }

    [Fact]
    public void Boundary_at_ten_minutes()
    {
        var below = new CallRecord("R-1", "US", 9.99, true);
        var exact = new CallRecord("R-2", "US", 10.0, true);
        Assert.Equal(449.55m, CallPricing.CalculateCost(in below));
        Assert.Equal(1200.00m, CallPricing.CalculateCost(in exact));
    }
}