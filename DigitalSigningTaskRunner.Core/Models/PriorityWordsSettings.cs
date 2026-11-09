namespace DigitalSigningTaskRunner.Core.Models
{
    public sealed record PriorityWordsSettings
    {
        public List<string> Keys { get; init; } = [];
    }
}