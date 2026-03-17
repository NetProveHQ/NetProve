using System;
using System.Threading;
using System.Threading.Tasks;
using NetProve.Core;

namespace NetProve.Engines
{
    /// <summary>
    /// Automatic background optimization engine.
    /// Monitors metrics and applies safe optimizations silently.
    /// Designed to not interfere with gaming/streaming.
    /// </summary>
    public sealed class AutoOptimizer
    {
        private bool _enabled;
        private readonly SemaphoreSlim _guard = new(1, 1);

        // Cooldown tracking
        private DateTime _lastRamOptimize = DateTime.MinValue;
        private DateTime _lastDnsFlush = DateTime.MinValue;
        private DateTime _lastProcessThrottle = DateTime.MinValue;
        private bool _tcpApplied;
        private bool _powerPlanSet;
        private bool _nagleDisabled;

        private const int RamCooldownSec = 120;
        private const int ProcessCooldownSec = 60;
        private const int DnsFlushIntervalMin = 30;

        public bool IsEnabled => _enabled;

        public void Enable()
        {
            if (_enabled) return;
            _enabled = true;

            // Subscribe to metrics events
            EventBus.Instance.Subscribe<SystemMetricsUpdatedEvent>(OnSystemMetrics);
            EventBus.Instance.Subscribe<GameDetectedEvent>(OnGameDetected);
            EventBus.Instance.Subscribe<GameEndedEvent>(OnGameEnded);

            // Apply TCP optimizations immediately
            if (!_tcpApplied)
            {
                _ = Task.Run(async () =>
                {
                    await CoreEngine.Instance.NetworkOptimizer.ApplyTcpOptimizationsAsync();
                    _tcpApplied = true;
                });
            }

            EventBus.Instance.Publish(new AutoModeChangedEvent { Enabled = true });
        }

        public void Disable()
        {
            if (!_enabled) return;
            _enabled = false;

            EventBus.Instance.Unsubscribe<SystemMetricsUpdatedEvent>(OnSystemMetrics);
            EventBus.Instance.Unsubscribe<GameDetectedEvent>(OnGameDetected);
            EventBus.Instance.Unsubscribe<GameEndedEvent>(OnGameEnded);

            // Restore any changes
            _ = Task.Run(async () =>
            {
                if (_powerPlanSet)
                {
                    await CoreEngine.Instance.PowerPlan.RestoreOriginalPlanAsync();
                    _powerPlanSet = false;
                }
                if (_nagleDisabled)
                {
                    await CoreEngine.Instance.AdapterOptimizer.EnableNagleAsync();
                    _nagleDisabled = false;
                }
                await CoreEngine.Instance.PowerPlan.RestoreVisualEffectsAsync();
            });

            EventBus.Instance.Publish(new AutoModeChangedEvent { Enabled = false });
        }

        private void OnSystemMetrics(SystemMetricsUpdatedEvent e)
        {
            if (!_enabled) return;
            if (!_guard.Wait(0)) return; // non-blocking, skip if already running

            _ = Task.Run(async () =>
            {
                try
                {
                    var m = e.Metrics;
                    var s = AppSettings.Instance;
                    var now = DateTime.Now;

                    // Auto RAM optimization
                    if (m.RamUsagePercent >= s.RamPressureThresholdPercent &&
                        (now - _lastRamOptimize).TotalSeconds > RamCooldownSec)
                    {
                        _lastRamOptimize = now;
                        await CoreEngine.Instance.RAMManager.OptimizeAsync();
                    }

                    // Auto process throttle during gaming/streaming with high CPU
                    bool anyModeActive = CoreEngine.Instance.Optimization.GamingModeActive ||
                                        CoreEngine.Instance.Optimization.StreamingModeActive;
                    if (anyModeActive &&
                        m.CpuUsagePercent >= s.CpuOverloadThresholdPercent &&
                        (now - _lastProcessThrottle).TotalSeconds > ProcessCooldownSec)
                    {
                        _lastProcessThrottle = now;
                        await CoreEngine.Instance.ProcessManager.ThrottleBackgroundProcessesAsync();
                    }

                    // Periodic DNS flush
                    if ((now - _lastDnsFlush).TotalMinutes > DnsFlushIntervalMin)
                    {
                        _lastDnsFlush = now;
                        await CoreEngine.Instance.NetworkOptimizer.FlushDnsAsync();
                    }
                }
                catch { /* Silent failure - don't interrupt user */ }
                finally { _guard.Release(); }
            });
        }

        private void OnGameDetected(GameDetectedEvent e)
        {
            if (!_enabled) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    // Switch to High Performance power plan
                    if (!_powerPlanSet)
                    {
                        await CoreEngine.Instance.PowerPlan.SetHighPerformanceAsync();
                        _powerPlanSet = true;
                    }

                    // Disable Nagle for lower latency
                    if (!_nagleDisabled)
                    {
                        await CoreEngine.Instance.AdapterOptimizer.DisableNagleAsync();
                        _nagleDisabled = true;
                    }

                    // Reduce visual effects
                    await CoreEngine.Instance.PowerPlan.ReduceVisualEffectsAsync();

                    // Flush DNS for fresh routing
                    await CoreEngine.Instance.NetworkOptimizer.FlushDnsAsync();

                    // Clear standby RAM
                    await CoreEngine.Instance.RAMManager.OptimizeAsync();
                }
                catch { }
            });
        }

        private void OnGameEnded(GameEndedEvent e)
        {
            if (!_enabled) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    // Restore power plan
                    if (_powerPlanSet)
                    {
                        await CoreEngine.Instance.PowerPlan.RestoreOriginalPlanAsync();
                        _powerPlanSet = false;
                    }

                    // Restore Nagle
                    if (_nagleDisabled)
                    {
                        await CoreEngine.Instance.AdapterOptimizer.EnableNagleAsync();
                        _nagleDisabled = false;
                    }

                    // Restore visual effects
                    await CoreEngine.Instance.PowerPlan.RestoreVisualEffectsAsync();

                    // Restore process priorities
                    await CoreEngine.Instance.ProcessManager.RestoreProcessPrioritiesAsync();
                }
                catch { }
            });
        }
    }
}
