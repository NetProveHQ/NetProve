using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NetProve.Core;
using NetProve.Models;

namespace NetProve.Engines
{
    /// <summary>
    /// Generates post-session performance reports from collected GameSession data.
    /// Persists reports to disk for historical review.
    /// </summary>
    public sealed class PerformanceReportEngine
    {
        private static readonly string ReportsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetProve", "Reports");

        private readonly List<PerformanceReport> _history = new();

        public PerformanceReportEngine()
        {
            Directory.CreateDirectory(ReportsDir);
            LoadHistory();

            EventBus.Instance.Subscribe<GameEndedEvent>(_ =>
            {
                var session = CoreEngine.Instance.GameDetector.CurrentSession;
                if (session != null) GenerateReport(session);
            });
        }

        public PerformanceReport GenerateReport(GameSession session)
        {
            var ping = session.PingSamples;
            var jitter = session.JitterSamples;
            var pl = session.PacketLossSamples;
            var cpu = session.CpuSamples;
            var ram = session.RamSamples;

            double avgPing = ping.Count > 0 ? ping.Average() : 0;
            double minPing = ping.Count > 0 ? ping.Min() : 0;
            double maxPing = ping.Count > 0 ? ping.Max() : 0;
            double avgJitter = jitter.Count > 0 ? jitter.Average() : 0;
            double avgPl = pl.Count > 0 ? pl.Average() : 0;
            float avgCpu = cpu.Count > 0 ? cpu.Average() : 0;
            float avgRam = ram.Count > 0 ? ram.Average() : 0;
            float peakCpu = cpu.Count > 0 ? cpu.Max() : 0;
            float peakRam = ram.Count > 0 ? ram.Max() : 0;

            // Determine quality rating
            string rating, emoji;
            double score = 100;
            if (avgPl >= 5) score -= 40; else if (avgPl >= 2) score -= 20;
            if (avgPing >= 150) score -= 30; else if (avgPing >= 80) score -= 15;
            if (avgJitter >= 30) score -= 20; else if (avgJitter >= 15) score -= 10;
            if (session.LagSpikeCount > 10) score -= 20; else if (session.LagSpikeCount > 5) score -= 10;

            if (score >= 90) { rating = "Excellent"; emoji = "★★★★★"; }
            else if (score >= 75) { rating = "Good"; emoji = "★★★★☆"; }
            else if (score >= 55) { rating = "Fair"; emoji = "★★★☆☆"; }
            else if (score >= 35) { rating = "Poor"; emoji = "★★☆☆☆"; }
            else { rating = "Very Poor"; emoji = "★☆☆☆☆"; }

            // Build suggestions
            var suggestions = new List<string>();
            if (avgPl >= 2)
                suggestions.Add("High packet loss detected – consider switching to a wired connection.");
            if (avgPing >= 80)
                suggestions.Add("Average latency is high – try servers closer to your region.");
            if (avgJitter >= 20)
                suggestions.Add("Unstable ping detected – check for background downloads or Wi-Fi interference.");
            if (session.LagSpikeCount > 5)
                suggestions.Add("Multiple lag spikes occurred – enable Gaming Mode for the next session.");
            if (peakCpu >= 90)
                suggestions.Add("CPU peaked at maximum – consider closing non-essential applications.");
            if (peakRam >= 90)
                suggestions.Add("RAM was near capacity – use the RAM Optimizer before gaming.");
            if (suggestions.Count == 0)
                suggestions.Add("Great session! Performance was stable throughout.");

            var report = new PerformanceReport
            {
                GameName = session.GameName,
                Platform = session.Platform,
                SessionStart = session.StartTime,
                SessionEnd = session.EndTime ?? DateTime.Now,
                Duration = session.Duration,
                AvgPingMs = Math.Round(avgPing, 1),
                MinPingMs = Math.Round(minPing, 1),
                MaxPingMs = Math.Round(maxPing, 1),
                AvgJitterMs = Math.Round(avgJitter, 1),
                AvgPacketLossPercent = Math.Round(avgPl, 2),
                AvgCpuPercent = MathF.Round(avgCpu, 1),
                AvgRamPercent = MathF.Round(avgRam, 1),
                PeakCpuPercent = MathF.Round(peakCpu, 1),
                PeakRamPercent = MathF.Round(peakRam, 1),
                LagSpikeCount = session.LagSpikeCount,
                BottlenecksDetected = session.DetectedBottlenecks.Distinct().ToList(),
                Suggestions = suggestions.ToArray(),
                OverallRating = rating,
                RatingEmoji = emoji
            };

            SaveReport(report);
            _history.Insert(0, report);
            return report;
        }

        public IReadOnlyList<PerformanceReport> GetHistory() => _history.AsReadOnly();

        private void SaveReport(PerformanceReport report)
        {
            try
            {
                var name = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var path = Path.Combine(ReportsDir, name);
                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private void LoadHistory()
        {
            try
            {
                foreach (var f in Directory.GetFiles(ReportsDir, "*.json")
                    .OrderByDescending(f => f).Take(20))
                {
                    try
                    {
                        var json = File.ReadAllText(f);
                        var r = JsonSerializer.Deserialize<PerformanceReport>(json);
                        if (r != null) _history.Add(r);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
