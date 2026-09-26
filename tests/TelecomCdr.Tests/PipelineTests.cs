namespace TelecomCdr.Tests;

public class PipelineTests
{
    private static CallRecord[] BuildRecords(int count)
    {
        string[] countries = { "KZ", "US", "DE", "XX", "kz" };
        var records = new CallRecord[count];
        for (int i = 0; i < count; i++)
        {
            bool roaming = i % 3 == 0;
            double minutes = (i * 7 % 61) * 0.25;   // 0.00 to 15.00, exact in binary
            records[i] = new CallRecord($"R-{i}", countries[i % 5], minutes, roaming);
        }
        return records;
    }

    [Fact]
    public void Parallel_equals_sequential_100_runs()
    {
        CallRecord[] records = BuildRecords(1200);
        CallRecord[] snapshot = (CallRecord[])records.Clone();
        decimal expected = CallProcessing.ProcessCallsSequential(records);

        for (int run = 0; run < 100; run++)
            Assert.Equal(expected, CallProcessing.ProcessCallsParallel(records));

        Assert.Equal(snapshot, records);
    }

    [Fact]
    public void Empty_array_returns_zero() =>
        Assert.Equal(0m, CallProcessing.ProcessCallsParallel(Array.Empty<CallRecord>()));

    [Fact]
    public void Odd_length_is_rejected()
    {
        var odd = new[] { new CallRecord("R-1", "KZ", 1.0, false) };
        Assert.Throws<ArgumentException>(() => CallProcessing.ProcessCallsParallel(odd));
    }

    [Fact]
    public void Null_is_rejected() =>
        Assert.Throws<ArgumentNullException>(() => CallProcessing.ProcessCallsParallel(null!));

    [Fact]
    public void Worker_failure_reaches_caller()
    {
        var records = new[] { default(CallRecord), new CallRecord("R-1", "KZ", 4.0, false) };
        Assert.Throws<ArgumentException>(() => CallProcessing.ProcessCallsParallel(records));
    }
}
