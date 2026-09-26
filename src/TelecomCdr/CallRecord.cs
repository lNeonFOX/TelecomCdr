namespace TelecomCdr;

public readonly record struct CallRecord
{
    public const double MaxDurationMinutes = 10_000.0;

    public string RecordId { get; }
    public string DestinationCountry { get; }
    public double DurationMinutes { get; }
    public bool IsRoaming { get; }

    public CallRecord(string recordId, string destinationCountry, double durationMinutes, bool isRoaming)
    {
        if (string.IsNullOrWhiteSpace(recordId))
            throw new ArgumentException("RecordId is blank.", nameof(recordId));
        if (string.IsNullOrWhiteSpace(destinationCountry))
            throw new ArgumentException("DestinationCountry is blank.", nameof(destinationCountry));
        if (double.IsNaN(durationMinutes) || double.IsInfinity(durationMinutes)
            || durationMinutes < 0.0 || durationMinutes > MaxDurationMinutes)
            throw new ArgumentException("DurationMinutes must be finite and in [0, 10000].", nameof(durationMinutes));

        RecordId = recordId;
        DestinationCountry = destinationCountry;
        DurationMinutes = durationMinutes;
        IsRoaming = isRoaming;
    }
}