using System;
using System.Diagnostics;

namespace NetProve.Models
{
    public sealed class ProcessInfo
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public float CpuPercent { get; set; }
        public long MemoryBytes { get; init; }
        public float MemoryMb => MemoryBytes / 1_048_576f;
        public ProcessPriorityClass Priority { get; init; }
        public bool IsWhitelisted { get; init; }
        public bool IsCritical { get; init; }
        public DateTime StartTime { get; init; }
        public bool IsThrottled { get; set; }
    }
}
