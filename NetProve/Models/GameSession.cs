using System;
using System.Collections.Generic;
using NetProve.Models;

namespace NetProve.Models
{
    public sealed class GameSession
    {
        public string GameName { get; init; } = "";
        public string Platform { get; init; } = "";
        public int ProcessId { get; init; }
        public DateTime StartTime { get; init; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;

        // Performance samples collected during session
        public List<double> PingSamples { get; } = new();
        public List<double> JitterSamples { get; } = new();
        public List<double> PacketLossSamples { get; } = new();
        public List<float> CpuSamples { get; } = new();
        public List<float> RamSamples { get; } = new();
        public int LagSpikeCount { get; set; }
        public List<LagCause> DetectedBottlenecks { get; } = new();
        public bool IsActive => EndTime == null;
    }
}
