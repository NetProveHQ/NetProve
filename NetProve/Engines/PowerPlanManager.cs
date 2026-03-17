using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetProve.Core;

namespace NetProve.Engines
{
    /// <summary>
    /// Manages Windows power plans and visual effects for gaming performance.
    /// All changes are fully reversible.
    /// </summary>
    public sealed class PowerPlanManager
    {
        private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        private bool _visualEffectsReduced;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, int pvParam, uint fWinIni);

        private const uint SPI_SETDRAGFULLWINDOWS = 0x0025;
        private const uint SPI_SETMENUANIMATION = 0x1003;
        private const uint SPI_SETCOMBOBOXANIMATION = 0x1005;
        private const uint SPI_SETLISTBOXSMOOTHSCROLLING = 0x1007;
        private const uint SPI_SETCLIENTAREAANIMATION = 0x1043;
        private const uint SPIF_SENDCHANGE = 0x02;

        /// <summary>Gets the currently active power plan GUID.</summary>
        public async Task<string> GetActivePlanGuidAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("powercfg", "/getactivescheme")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using var p = Process.Start(psi);
                    var output = p?.StandardOutput.ReadToEnd() ?? "";
                    p?.WaitForExit(5000);

                    var match = Regex.Match(output, @"([0-9a-fA-F\-]{36})");
                    return match.Success ? match.Groups[1].Value : "";
                }
                catch { return ""; }
            });
        }

        /// <summary>Switch to High Performance power plan, saving the current one.</summary>
        public async Task<bool> SetHighPerformanceAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var currentGuid = await GetActivePlanGuidAsync();
                    if (string.Equals(currentGuid, HighPerformanceGuid, StringComparison.OrdinalIgnoreCase))
                        return true; // Already on high performance

                    // Save original plan for later restoration
                    if (!string.IsNullOrEmpty(currentGuid))
                    {
                        AppSettings.Instance.OriginalPowerPlanGuid = currentGuid;
                        AppSettings.Instance.Save();
                    }

                    RunPowerCfg($"/setactive {HighPerformanceGuid}");

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Power Plan",
                        Description = "Switched to High Performance power plan."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>Restore the original power plan.</summary>
        public async Task<bool> RestoreOriginalPlanAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var guid = AppSettings.Instance.OriginalPowerPlanGuid;
                    if (string.IsNullOrEmpty(guid)) return false;

                    RunPowerCfg($"/setactive {guid}");

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Power Plan",
                        Description = "Restored original power plan."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>Reduce Windows visual effects for better gaming performance.</summary>
        public Task<bool> ReduceVisualEffectsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_visualEffectsReduced) return true;

                    SystemParametersInfo(SPI_SETDRAGFULLWINDOWS, 0, 0, SPIF_SENDCHANGE);
                    SystemParametersInfo(SPI_SETMENUANIMATION, 0, 0, SPIF_SENDCHANGE);
                    SystemParametersInfo(SPI_SETCOMBOBOXANIMATION, 0, 0, SPIF_SENDCHANGE);
                    SystemParametersInfo(SPI_SETLISTBOXSMOOTHSCROLLING, 0, 0, SPIF_SENDCHANGE);
                    SystemParametersInfo(SPI_SETCLIENTAREAANIMATION, 0, 0, SPIF_SENDCHANGE);

                    _visualEffectsReduced = true;

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Visual Effects",
                        Description = "Windows animations reduced for gaming performance."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        /// <summary>Restore Windows visual effects.</summary>
        public Task<bool> RestoreVisualEffectsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!_visualEffectsReduced) return true;

                    SystemParametersInfo(SPI_SETDRAGFULLWINDOWS, 1, 0, SPIF_SENDCHANGE);
                    SystemParametersInfo(SPI_SETMENUANIMATION, 0, 1, SPIF_SENDCHANGE);
                    SystemParametersInfo(SPI_SETCOMBOBOXANIMATION, 0, 1, SPIF_SENDCHANGE);
                    SystemParametersInfo(SPI_SETLISTBOXSMOOTHSCROLLING, 0, 1, SPIF_SENDCHANGE);
                    SystemParametersInfo(SPI_SETCLIENTAREAANIMATION, 0, 1, SPIF_SENDCHANGE);

                    _visualEffectsReduced = false;

                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Visual Effects",
                        Description = "Windows animations restored."
                    });
                    return true;
                }
                catch { return false; }
            });
        }

        private static void RunPowerCfg(string args)
        {
            var psi = new ProcessStartInfo("powercfg", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(5000);
        }
    }
}
