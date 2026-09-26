using System.Globalization;
using TelecomCdr;

Console.WriteLine("Telecom CDR pipeline — enter call records.");
Console.WriteLine("Leave RecordId empty to stop entering records.");
Console.WriteLine();

var records = new List<CallRecord>();

while (true)
{
    Console.Write("RecordId (or press Enter to finish): ");
    string? recordId = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(recordId))
        break;

    Console.Write("Destination country (2-letter code, e.g. KZ): ");
    string? country = Console.ReadLine();

    Console.Write("Duration in minutes (0 to 10000): ");
    string? durationInput = Console.ReadLine();

    Console.Write("Is roaming? (y/n): ");
    string? roamingInput = Console.ReadLine();

    if (!double.TryParse(durationInput, NumberStyles.Float, CultureInfo.InvariantCulture, out double duration))
    {
        Console.WriteLine("  -> Invalid number, record skipped.\n");
        continue;
    }

    bool isRoaming = roamingInput?.Trim().ToLowerInvariant() is "y" or "yes";

    try
    {
        var record = new CallRecord(recordId, country ?? "", duration, isRoaming);
        records.Add(record);

        decimal cost = CallPricing.CalculateCost(in record);
        Console.WriteLine($"  -> Added. Cost for this call: {cost.ToString("F2", CultureInfo.InvariantCulture)} KZT\n");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"  -> Rejected: {ex.Message}\n");
    }
}

if (records.Count == 0)
{
    Console.WriteLine("No valid records entered. Exiting.");
    return;
}

CallRecord[] recordArray = records.ToArray();

Console.WriteLine("----------------------------------------");
decimal sequentialTotal = CallProcessing.ProcessCallsSequential(recordArray);
Console.WriteLine($"Sequential total: {sequentialTotal.ToString("F2", CultureInfo.InvariantCulture)} KZT");

if (recordArray.Length % 2 == 0)
{
    decimal parallelTotal = CallProcessing.ProcessCallsParallel(recordArray);
    Console.WriteLine($"Parallel total:   {parallelTotal.ToString("F2", CultureInfo.InvariantCulture)} KZT");
    Console.WriteLine($"Totals equal:     {sequentialTotal == parallelTotal}");
}
else
{
    Console.WriteLine($"Parallel total:   skipped (record count is odd: {recordArray.Length}; ProcessCallsParallel requires an even number)");
}