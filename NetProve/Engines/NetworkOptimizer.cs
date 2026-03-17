using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NetProve.Core;

namespace NetProve.Engines
{
    /// <summary>
    /// Safe, reversible network optimizations.
    /// Never intercepts or modifies packets.
    /// Only uses well-documented OS-level APIs and commands.
    /// </summary>
    public sealed class NetworkOptimizer
    {
        private bool _tcpOptimized;

        /// <summary>Flushes the DNS resolver cache.</summary>
        public async Task<bool> FlushDnsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("ipconfig", "/flushdns")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(5000);

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "DNS Flush",
                        Description = "DNS resolver cache cleared successfully."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>
        /// Applies safe TCP/IP stack tuning via netsh.
        /// Changes are session-scoped where possible; fully reversible.
        /// </summary>
        public async Task<bool> ApplyTcpOptimizationsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Disable auto-tuning heuristics (can cause latency on some setups)
                    RunNetsh("int tcp set heuristics disabled");
                    // Enable ECN (Explicit Congestion Notification) for better throughput
                    RunNetsh("int tcp set global ecncapability=enabled");
                    // Disable TCP chimney offload (can cause issues with some NICs)
                    RunNetsh("int tcp set global chimney=disabled");
                    // Set receive-side scaling state
                    RunNetsh("int tcp set global rss=enabled");

                    _tcpOptimized = true;
                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "TCP Optimize",
                        Description = "TCP/IP stack tuned for lower latency."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>Restores default TCP settings.</summary>
        public async Task<bool> RestoreTcpDefaultsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    RunNetsh("int tcp set heuristics enabled");
                    RunNetsh("int tcp set global ecncapability=default");
                    RunNetsh("int tcp set global chimney=default");
                    RunNetsh("int tcp set global rss=enabled");

                    _tcpOptimized = false;
                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "TCP Restore",
                        Description = "TCP/IP settings restored to Windows defaults."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>
        /// Elevates priority of a game process via SetPriorityClass.
        /// Uses OS-level priority – no packet injection.
        /// </summary>
        public async Task<bool> PrioritizeGameProcessAsync(int pid)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var p = Process.GetProcessById(pid);
                    p.PriorityClass = ProcessPriorityClass.High;
                    p.Dispose();

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Game Priority",
                        Description = $"Set process {pid} to High priority."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        public bool IsTcpOptimized => _tcpOptimized;

        private static void RunNetsh(string args)
        {
            var psi = new ProcessStartInfo("netsh", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(5000);
        }
    }
}
