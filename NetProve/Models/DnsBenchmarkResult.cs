namespace NetProve.Models
{
    public sealed class DnsBenchmarkResult
    {
        public string Name { get; init; } = "";
        public string PrimaryIp { get; init; } = "";
        public string SecondaryIp { get; init; } = "";
        public double AverageMs { get; init; }
    }
}
