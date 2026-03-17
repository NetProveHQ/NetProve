using System;
using System.Collections.Generic;

namespace NetProve.Models
{
    public sealed class PerformanceReport
    {
        public string GameName { get; init; } = "";
        public string Platform { get; init; } = "";
        public DateTime SessionStart { get; init; }
        public DateTime SessionEnd { get; init; }
        public TimeSpan Duration { get; init; }

        // Network
        public double AvgPingMs { get; init; }
        public double MinPingMs { get; init; }
        public double MaxPingMs { get; init; }
        public double AvgJitterMs { get; init; }
        public double AvgPacketLossPercent { get; init; }

        // System
        public float AvgCpuPercent { get; init; }
        public float AvgRamPercent { get; init; }
        public float PeakCpuPercent { get; init; }
        public float PeakRamPercent { get; init; }

        // Events
        public int LagSpikeCount { get; init; }
        public List<LagCause> BottlenecksDetected { get; init; } = new();
        public string[] Suggestions { get; init; } = Array.Empty<string>();

        // Quality rating
        public string OverallRating { get; init; } = "";
        public string RatingEmoji { get; init; } = "";
    }
}
