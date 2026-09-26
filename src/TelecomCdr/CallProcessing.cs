using System.Runtime.ExceptionServices;

namespace TelecomCdr;

public static class CallProcessing
{
    public static decimal ProcessCallsSequential(CallRecord[] records)
    {
        ArgumentNullException.ThrowIfNull(records);

        decimal total = 0m;
        foreach (var record in records)
            total += CallPricing.CalculateCost(in record);
        return total;
    }
    
     public static decimal ProcessCallsParallel(CallRecord[] records)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (records.Length % 2 != 0)
            throw new ArgumentException("Length must be even.", nameof(records));
        if (records.Length == 0)
            return 0m;

        int mid = records.Length / 2;
        CallRecord[] firstInput = records[..mid];
        CallRecord[] secondInput = records[mid..];

        decimal[] firstOutput = new decimal[firstInput.Length];
        decimal[] secondOutput = new decimal[secondInput.Length];

        var firstWorker = new Worker(firstInput, firstOutput);
        var secondWorker = new Worker(secondInput, secondOutput);

        var firstThread = new Thread(firstWorker.Run);
        var secondThread = new Thread(secondWorker.Run);

        firstThread.Start();
        secondThread.Start();
        firstThread.Join();
        secondThread.Join();

        if (firstWorker.Error is not null && secondWorker.Error is not null)
            throw new AggregateException("Both workers failed.", firstWorker.Error, secondWorker.Error);
        if (firstWorker.Error is not null)
            ExceptionDispatchInfo.Capture(firstWorker.Error).Throw();
        if (secondWorker.Error is not null)
            ExceptionDispatchInfo.Capture(secondWorker.Error).Throw();

        decimal total = 0m;
        foreach (decimal cost in firstOutput) total += cost;
        foreach (decimal cost in secondOutput) total += cost;
        return total;
    }

    private sealed class Worker
    {
        private readonly CallRecord[] _input;
        private readonly decimal[] _output;
        public Exception? Error { get; private set; }

        public Worker(CallRecord[] input, decimal[] output)
        {
            _input = input;
            _output = output;
        }

        public void Run()
        {
            try
            {
                for (int i = 0; i < _input.Length; i++)
                    _output[i] = CallPricing.CalculateCost(in _input[i]);
            }
            catch (Exception ex)
            {
                Error = ex;
            }
        }
    }
}