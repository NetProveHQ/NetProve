using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.Win32;
using NetProve.Core;

namespace NetProve.Engines
{
    /// <summary>
    /// Optimizes network adapter settings: Nagle algorithm, Wi-Fi band detection,
    /// network stack reset. All changes are reversible.
    /// </summary>
    public sealed class NetworkAdapterOptimizer
    {
        private const string TcpipInterfacesKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";

        /// <summary>
        /// Disable Nagle algorithm on all network interfaces for lower latency.
        /// Sets TcpAckFrequency=1 and TCPNoDelay=1 on each interface.
        /// </summary>
        public Task<bool> DisableNagleAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    using var interfaces = Registry.LocalMachine.OpenSubKey(TcpipInterfacesKey, false);
                    if (interfaces == null) return false;

                    foreach (var subKeyName in interfaces.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = Registry.LocalMachine.OpenSubKey(
                                $@"{TcpipInterfacesKey}\{subKeyName}", true);
                            if (subKey == null) continue;

                            subKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                            subKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                        }
                        catch { /* Skip interfaces we can't modify */ }
                    }

                    AppSettings.Instance.NagleDisabled = true;
                    AppSettings.Instance.Save();

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Nagle Disabled",
                        Description = "Nagle algorithm disabled on all interfaces for lower latency."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>
        /// Re-enable Nagle algorithm by removing custom registry values.
        /// </summary>
        public Task<bool> EnableNagleAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    using var interfaces = Registry.LocalMachine.OpenSubKey(TcpipInterfacesKey, false);
                    if (interfaces == null) return false;

                    foreach (var subKeyName in interfaces.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = Registry.LocalMachine.OpenSubKey(
                                $@"{TcpipInterfacesKey}\{subKeyName}", true);
                            if (subKey == null) continue;

                            subKey.DeleteValue("TcpAckFrequency", false);
                            subKey.DeleteValue("TCPNoDelay", false);
                        }
                        catch { }
                    }

                    AppSettings.Instance.NagleDisabled = false;
                    AppSettings.Instance.Save();

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Nagle Enabled",
                        Description = "Nagle algorithm restored to default on all interfaces."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>Detect current Wi-Fi band (2.4 GHz vs 5 GHz).</summary>
        public async Task<string> DetectWifiBandAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("netsh", "wlan show interfaces")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using var p = Process.Start(psi);
                    var output = p?.StandardOutput.ReadToEnd() ?? "";
                    p?.WaitForExit(5000);

                    // Parse channel or radio type
                    foreach (var line in output.Split('\n'))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("Radio type", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("Radyo t", StringComparison.OrdinalIgnoreCase))
                        {
                            if (trimmed.Contains("802.11a") || trimmed.Contains("802.11ac") ||
                                trimmed.Contains("802.11ax") || trimmed.Contains("802.11n") && trimmed.Contains("5"))
                                return "5 GHz (802.11ac/ax)";
                            if (trimmed.Contains("802.11b") || trimmed.Contains("802.11g"))
                                return "2.4 GHz (802.11b/g)";
                            return trimmed.Contains(":") ? trimmed.Split(':').Last().Trim() : "Unknown";
                        }
                        if (trimmed.StartsWith("Channel", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("Kanal", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(trimmed.Split(':').Last().Trim(), out int ch))
                                return ch >= 36 ? $"5 GHz (Channel {ch})" : $"2.4 GHz (Channel {ch})";
                        }
                    }

                    // Check if no Wi-Fi at all
                    var wifiAdapter = NetworkInterface.GetAllNetworkInterfaces()
                        .Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                                   ni.OperationalStatus == OperationalStatus.Up);
                    return wifiAdapter ? "Unknown Band" : "Ethernet (No Wi-Fi)";
                }
                catch { return "Detection failed"; }
            });
        }

        /// <summary>Full network stack reset (requires system restart to take effect).</summary>
        public async Task<bool> ResetNetworkStackAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    RunCmd("netsh", "winsock reset");
                    RunCmd("netsh", "int ip reset");
                    RunCmd("ipconfig", "/release");
                    RunCmd("ipconfig", "/renew");
                    RunCmd("ipconfig", "/flushdns");

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Network Reset",
                        Description = "Network stack reset. Restart recommended for full effect."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        private static void RunCmd(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(10000);
        }
    }
}
