namespace DigitalSigningTaskRunner.Core.Models
{
    // [no tocar 🔒]
    public sealed record TaskRunnerSettings // uso de null! por principio de fail-fast
    {
        public int IntervalInSeconds { get; init; }
        public int BatchSize { get; init; }
        public string LogDirectoryPath { get; init; } = null!;
    }
}