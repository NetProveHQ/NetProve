using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using NetProve.Core;
using NetProve.Models;

namespace NetProve.Engines
{
    /// <summary>
    /// Benchmarks popular DNS servers and can apply the fastest one.
    /// </summary>
    public sealed class DnsBenchmark
    {
        private static readonly (string Name, string Primary, string Secondary)[] DnsServers =
        {
            ("Google", "8.8.8.8", "8.8.4.4"),
            ("Cloudflare", "1.1.1.1", "1.0.0.1"),
            ("OpenDNS", "208.67.222.222", "208.67.220.220"),
            ("Quad9", "9.9.9.9", "149.112.112.112"),
            ("AdGuard", "94.140.14.14", "94.140.15.15"),
        };

        /// <summary>Benchmark all DNS servers by pinging each 5 times.</summary>
        public async Task<List<DnsBenchmarkResult>> BenchmarkAsync()
        {
            var results = new List<DnsBenchmarkResult>();

            foreach (var (name, primary, secondary) in DnsServers)
            {
                try
                {
                    var times = new List<double>();
                    using var pinger = new Ping();

                    for (int i = 0; i < 5; i++)
                    {
                        var reply = await pinger.SendPingAsync(primary, 3000);
                        if (reply.Status == IPStatus.Success)
                            times.Add(reply.RoundtripTime);
                        else
                            times.Add(3000); // timeout penalty
                    }

                    results.Add(new DnsBenchmarkResult
                    {
                        Name = name,
                        PrimaryIp = primary,
                        SecondaryIp = secondary,
                        AverageMs = times.Count > 0 ? times.Average() : 9999
                    });
                }
                catch
                {
                    results.Add(new DnsBenchmarkResult
                    {
                        Name = name,
                        PrimaryIp = primary,
                        SecondaryIp = secondary,
                        AverageMs = 9999
                    });
                }
            }

            return results.OrderBy(r => r.AverageMs).ToList();
        }

        /// <summary>Get the name of the active network adapter.</summary>
        public string GetActiveAdapterName()
        {
            try
            {
                var iface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni =>
                        ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        ni.GetIPProperties().GatewayAddresses.Count > 0);
                return iface?.Name ?? "Ethernet";
            }
            catch { return "Ethernet"; }
        }

        /// <summary>Apply a specific DNS server to the active adapter.</summary>
        public async Task<bool> ApplyDnsAsync(string primary, string secondary)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var adapter = GetActiveAdapterName();
                    RunNetsh($"interface ip set dns name=\"{adapter}\" static {primary}");
                    RunNetsh($"interface ip add dns name=\"{adapter}\" {secondary} index=2");

                    AppSettings.Instance.PreferredDns = primary;
                    AppSettings.Instance.Save();

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "DNS Applied",
                        Description = $"DNS set to {primary} / {secondary}."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>Restore DNS to automatic (DHCP).</summary>
        public async Task<bool> RestoreAutoDnsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var adapter = GetActiveAdapterName();
                    RunNetsh($"interface ip set dns name=\"{adapter}\" dhcp");

                    AppSettings.Instance.PreferredDns = "";
                    AppSettings.Instance.Save();

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "DNS Restore",
                        Description = "DNS restored to automatic (DHCP)."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

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
