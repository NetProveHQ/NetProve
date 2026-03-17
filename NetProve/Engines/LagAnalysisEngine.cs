using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetProve.Core;
using NetProve.Localization;
using NetProve.Models;

namespace NetProve.Engines
{
    /// <summary>
    /// Analyzes the current system and network state to identify
    /// root causes of lag or performance drops.
    /// </summary>
    public sealed class LagAnalysisEngine
    {
        public async Task<LagAnalysisResult> AnalyzeAsync()
        {
            return await Task.Run(() =>
            {
                var loc = LocalizationManager.Instance;
                var sys = CoreEngine.Instance.SystemMonitor.Latest;
                var net = CoreEngine.Instance.NetworkAnalyzer.Latest;

                if (sys == null || net == null)
                    return new LagAnalysisResult
                    {
                        Severity = LagSeverity.None,
                        Summary = LocalizationManager.Instance["InsufficientData"],
                        PrimaryCause = LagCause.None
                    };

                var causes = new List<LagCauseDetail>();
                var s = AppSettings.Instance;

                // ── CPU check ─────────────────────────────────────────────
                if (sys.CpuUsagePercent >= s.CpuOverloadThresholdPercent)
                {
                    causes.Add(new LagCauseDetail
                    {
                        Cause = LagCause.CpuBottleneck,
                        Description = string.Format(loc["LagCpuDesc"], sys.CpuUsagePercent),
                        Confidence = Math.Min(100f, sys.CpuUsagePercent),
                        Recommendations = new[]
                        {
                            loc["RecCpuClose"],
                            loc["RecCpuCheck"],
                            loc["RecCpuUpgrade"]
                        },
                        Metrics = new Dictionary<string, string>
                        {
                            [loc["MetCpuUsage"]] = $"{sys.CpuUsagePercent:F1}%",
                            [loc["MetCpuCores"]] = sys.CpuCores.ToString(),
                            [loc["MetCpu"]] = sys.CpuName
                        }
                    });
                }

                // ── RAM check ─────────────────────────────────────────────
                if (sys.RamUsagePercent >= s.RamPressureThresholdPercent)
                {
                    causes.Add(new LagCauseDetail
                    {
                        Cause = LagCause.RamPressure,
                        Description = string.Format(loc["LagRamDesc"], sys.RamUsagePercent),
                        Confidence = Math.Min(100f, sys.RamUsagePercent),
                        Recommendations = new[]
                        {
                            loc["RecRamOptimize"],
                            loc["RecRamClose"],
                            loc["RecRamAdd"]
                        },
                        Metrics = new Dictionary<string, string>
                        {
                            [loc["MetRamUsed"]] = $"{sys.UsedRamGb:F1} GB",
                            [loc["MetRamTotal"]] = $"{sys.TotalRamGb:F1} GB",
                            [loc["MetRamFree"]] = $"{sys.AvailableRamGb:F1} GB"
                        }
                    });
                }

                // ── Disk I/O check ────────────────────────────────────────
                if (sys.DiskActivityPercent >= s.DiskActivityThresholdPercent)
                {
                    causes.Add(new LagCauseDetail
                    {
                        Cause = LagCause.DiskIoBottleneck,
                        Description = string.Format(loc["LagDiskDesc"], sys.DiskActivityPercent),
                        Confidence = Math.Min(100f, sys.DiskActivityPercent),
                        Recommendations = new[]
                        {
                            loc["RecDiskPause"],
                            loc["RecDiskSsd"],
                            loc["RecDiskDefrag"]
                        },
                        Metrics = new Dictionary<string, string>
                        {
                            [loc["MetDiskActivity"]] = $"{sys.DiskActivityPercent:F1}%",
                            [loc["MetReadSpeed"]] = $"{sys.DiskReadBytesPerSec / 1_048_576f:F1} MB/s",
                            [loc["MetWriteSpeed"]] = $"{sys.DiskWriteBytesPerSec / 1_048_576f:F1} MB/s"
                        }
                    });
                }

                // ── Network latency check ─────────────────────────────────
                if (net.PingMs >= s.NetworkLatencyCriticalMs)
                {
                    causes.Add(new LagCauseDetail
                    {
                        Cause = LagCause.NetworkLatencySpike,
                        Description = string.Format(loc["LagNetDesc"], net.PingMs),
                        Confidence = Math.Min(100f, (float)net.PingMs / s.NetworkLatencyCriticalMs * 70f),
                        Recommendations = new[]
                        {
                            loc["RecNetWired"],
                            loc["RecNetCongestion"],
                            loc["RecNetDns"],
                            loc["RecNetIsp"]
                        },
                        Metrics = new Dictionary<string, string>
                        {
                            [loc["MetPing"]] = $"{net.PingMs:F0}ms",
                            [loc["MetJitter"]] = $"{net.JitterMs:F1}ms",
                        }
                    });
                }

                // ── Packet loss check ─────────────────────────────────────
                if (net.PacketLossPercent >= s.PacketLossWarningPercent)
                {
                    causes.Add(new LagCauseDetail
                    {
                        Cause = LagCause.PacketLoss,
                        Description = string.Format(loc["LagPLDesc"], net.PacketLossPercent),
                        Confidence = Math.Min(100f, (float)net.PacketLossPercent * 20f),
                        Recommendations = new[]
                        {
                            loc["RecPlCable"],
                            loc["RecPlWifi"],
                            loc["RecPlRouter"],
                            loc["RecPlTrace"]
                        },
                        Metrics = new Dictionary<string, string>
                        {
                            [loc["MetPacketLoss"]] = $"{net.PacketLossPercent:F1}%"
                        }
                    });
                }

                // ── Jitter check ──────────────────────────────────────────
                if (net.JitterMs >= s.JitterWarningMs && !causes.Any(c => c.Cause == LagCause.NetworkLatencySpike))
                {
                    causes.Add(new LagCauseDetail
                    {
                        Cause = LagCause.UnstableConnection,
                        Description = string.Format(loc["LagJitterDesc"], net.JitterMs),
                        Confidence = Math.Min(85f, (float)net.JitterMs * 2f),
                        Recommendations = new[]
                        {
                            loc["RecJitSignal"],
                            loc["RecJitInterference"],
                            loc["RecJitDriver"]
                        },
                        Metrics = new Dictionary<string, string>
                        {
                            [loc["MetJitter"]] = $"{net.JitterMs:F1}ms"
                        }
                    });
                }

                // ── Background interference ───────────────────────────────
                var procs = CoreEngine.Instance.ProcessManager.GetSnapshot();
                var heavyBg = procs.Where(p =>
                    !p.IsCritical && !p.IsWhitelisted &&
                    p.CpuPercent > 20 && p.MemoryMb > 200).ToList();

                if (heavyBg.Count > 0)
                {
                    causes.Add(new LagCauseDetail
                    {
                        Cause = LagCause.BackgroundInterference,
                        Description = string.Format(loc["LagBgDesc"], heavyBg.Count),
                        Confidence = Math.Min(100f, heavyBg.Count * 25f),
                        Recommendations = new[]
                        {
                            string.Format(loc["RecBgClose"], string.Join(", ", heavyBg.Take(3).Select(p => p.Name))),
                            loc["RecBgGaming"]
                        },
                        Metrics = new Dictionary<string, string>
                        {
                            [loc["MetHeavyProcs"]] = heavyBg.Count.ToString()
                        }
                    });
                }

                // ── Assemble result ───────────────────────────────────────
                LagSeverity severity = causes.Count == 0 ? LagSeverity.None :
                    causes.Any(c => c.Confidence >= 80) ? LagSeverity.High :
                    causes.Any(c => c.Confidence >= 50) ? LagSeverity.Medium :
                    LagSeverity.Low;

                LagCause primary = causes.Count > 0
                    ? causes.OrderByDescending(c => c.Confidence).First().Cause
                    : LagCause.None;

                string summary = causes.Count == 0
                    ? loc["LagNoCauses"]
                    : string.Format(loc["LagCausesFound"], causes.Count, FormatCause(primary));

                var allRecs = causes.SelectMany(c => c.Recommendations).Distinct().Take(6).ToArray();

                return new LagAnalysisResult
                {
                    Severity = severity,
                    PrimaryCause = primary,
                    Summary = summary,
                    Causes = causes,
                    Recommendations = allRecs,
                    CpuPercent = sys.CpuUsagePercent,
                    RamPercent = sys.RamUsagePercent,
                    DiskPercent = sys.DiskActivityPercent,
                    PingMs = net.PingMs,
                    JitterMs = net.JitterMs,
                    PacketLossPercent = net.PacketLossPercent
                };
            });
        }

        private static string FormatCause(LagCause c) => c switch
        {
            LagCause.CpuBottleneck => "CPU Bottleneck",
            LagCause.RamPressure => "RAM Pressure",
            LagCause.DiskIoBottleneck => "Disk I/O Bottleneck",
            LagCause.NetworkLatencySpike => "Network Latency Spike",
            LagCause.PacketLoss => "Packet Loss",
            LagCause.UnstableConnection => "Unstable Connection",
            LagCause.BackgroundInterference => "Background Interference",
            LagCause.Multiple => "Multiple Causes",
            _ => "None"
        };
    }
}
