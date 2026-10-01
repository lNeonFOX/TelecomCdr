namespace TelecomCdr;

public static class CallPricing
{
    public static decimal CalculateCost(in CallRecord record)
    {
        decimal rawCost = record switch
        {
            _ when string.IsNullOrWhiteSpace(record.RecordId)
                => throw new ArgumentException("Invalid RecordId.", nameof(record)),
            _ when string.IsNullOrWhiteSpace(record.DestinationCountry)
                => throw new ArgumentException("Invalid country.", nameof(record)),
            _ when double.IsNaN(record.DurationMinutes)
                => throw new ArgumentException("Duration is NaN.", nameof(record)),
            { DurationMinutes: < 0.0 or > CallRecord.MaxDurationMinutes }
                => throw new ArgumentException("Duration out of range.", nameof(record)),

            { IsRoaming: true, DestinationCountry: "KZ", DurationMinutes: < 1.0 } => 50.00m,
            { IsRoaming: false, DestinationCountry: "KZ" } => 15.00m * (decimal)record.DurationMinutes,
            { IsRoaming: true, DurationMinutes: >= 10.0 } => 120.00m * (decimal)record.DurationMinutes,
            _ => 45.00m * (decimal)record.DurationMinutes
        };

        return Math.Round(rawCost, 2, MidpointRounding.AwayFromZero);
    }
}
