using System.Globalization;
using TelecomCdr;

var records = new CallRecord[]
{
    new("A-001", "KZ", 4.0, false),
    new("A-002", "KZ", 0.5, true),
    new("A-003", "US", 10.0, true),
    new("A-004", "DE", 3.0, false),
};

decimal seq = CallProcessing.ProcessCallsSequential(records);
decimal par = CallProcessing.ProcessCallsParallel(records);
Console.WriteLine($"Sequential: {seq.ToString("F2", CultureInfo.InvariantCulture)}");
Console.WriteLine($"Parallel:   {par.ToString("F2", CultureInfo.InvariantCulture)}");