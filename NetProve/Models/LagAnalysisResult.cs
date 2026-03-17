using System;
using System.Collections.Generic;

namespace NetProve.Models
{
    public enum LagCause
    {
        None,
        CpuBottleneck,
        RamPressure,
        DiskIoBottleneck,
        NetworkLatencySpike,
        PacketLoss,
        UnstableConnection,
        BackgroundInterference,
        Multiple
    }

    public enum LagSeverity { None, Low, Medium, High, Critical }

    public sealed class LagCauseDetail
    {
        public LagCause Cause { get; init; }
        public string Description { get; init; } = "";
        public float Confidence { get; init; }   // 0–100
        public string[] Recommendations { get; init; } = Array.Empty<string>();
        public Dictionary<string, string> Metrics { get; init; } = new();
    }

    public sealed class LagAnalysisResult
    {
        public DateTime Timestamp { get; init; } = DateTime.Now;
        public LagSeverity Severity { get; init; }
        public LagCause PrimaryCause { get; init; }
        public string Summary { get; init; } = "";
        public List<LagCauseDetail> Causes { get; init; } = new();
        public string[] Recommendations { get; init; } = Array.Empty<string>();

        // Snapshot of metrics at analysis time
        public float CpuPercent { get; init; }
        public float RamPercent { get; init; }
        public float DiskPercent { get; init; }
        public double PingMs { get; init; }
        public double JitterMs { get; init; }
        public double PacketLossPercent { get; init; }
    }

    public sealed class LagPrediction
    {
        public DateTime Timestamp { get; init; } = DateTime.Now;
        public bool PredictedLag { get; init; }
        public LagSeverity PredictedSeverity { get; init; }
        public int EstimatedSecondsUntilLag { get; init; }
        public string Reason { get; init; } = "";
        public float Confidence { get; init; }  // 0–100
    }
}
