using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetProve.Core;
using NetProve.Models;

namespace NetProve.Managers
{
    /// <summary>
    /// Discovers running processes, tracks resource usage, and optionally
    /// throttles high-consuming background processes during Gaming/Streaming mode.
    /// Never terminates processes – only adjusts priority.
    /// </summary>
    public sealed class ProcessManager : IDisposable
    {
        private readonly Dictionary<int, ProcessPriorityClass> _originalPriorities = new();
        private readonly HashSet<int> _throttled = new();
        private readonly object _lock = new();

        private List<ProcessInfo> _snapshot = new();
        private CancellationTokenSource? _cts;
        private Task? _task;

        public IReadOnlyList<ProcessInfo> Snapshot => _snapshot;

        // ── Critical names: never touch ───────────────────────────────────────
        private static readonly HashSet<string> _neverTouch = new(StringComparer.OrdinalIgnoreCase)
        {
            "System","Idle","smss","csrss","wininit","winlogon","services",
            "lsass","lsm","svchost","MsMpEng","SecurityHealthService","dwm",
            "fontdrvhost","audiodg","conhost","NetProve","taskmgr","regsvc"
        };

        public void Start(CancellationToken externalToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _task = Task.Run(() => MonitorLoop(_cts.Token), _cts.Token);
        }

        public void Stop() { _cts?.Cancel(); try { _task?.Wait(2000); } catch { } }

        private async Task MonitorLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(AppSettings.Instance.ProcessPollIntervalMs, ct);
                    _snapshot = GetSnapshot();
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }

        public List<ProcessInfo> GetSnapshot()
        {
            var result = new List<ProcessInfo>();
            var procs = Process.GetProcesses();

            foreach (var p in procs)
            {
                try
                {
                    bool isCritical = _neverTouch.Contains(p.ProcessName);
                    bool isWhitelisted = AppSettings.Instance.WhitelistedProcesses
                        .Contains(p.ProcessName);

                    bool isThrottled;
                    lock (_lock) { isThrottled = _throttled.Contains(p.Id); }

                    result.Add(new ProcessInfo
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        Description = GetDescription(p),
                        MemoryBytes = p.WorkingSet64,
                        Priority = p.PriorityClass,
                        IsWhitelisted = isWhitelisted,
                        IsCritical = isCritical,
                        IsThrottled = isThrottled,
                        StartTime = TryGetStartTime(p)
                    });
                }
                catch { }
                finally { p.Dispose(); }
            }

            // Sort by memory desc
            result.Sort((a, b) => b.MemoryBytes.CompareTo(a.MemoryBytes));
            return result;
        }

        /// <summary>
        /// Lowers priority of background non-critical processes.
        /// Saves original priorities for later restoration.
        /// </summary>
        public async Task ThrottleBackgroundProcessesAsync()
        {
            await Task.Run(() =>
            {
                var procs = Process.GetProcesses();
                lock (_lock)
                {
                    foreach (var p in procs)
                    {
                        try
                        {
                            if (_neverTouch.Contains(p.ProcessName)) continue;
                            if (AppSettings.Instance.WhitelistedProcesses.Contains(p.ProcessName)) continue;
                            if (p.MainWindowHandle != IntPtr.Zero) continue; // skip foreground
                            if (_throttled.Contains(p.Id)) continue;

                            _originalPriorities[p.Id] = p.PriorityClass;
                            p.PriorityClass = ProcessPriorityClass.BelowNormal;
                            _throttled.Add(p.Id);
                        }
                        catch { }
                        finally { p.Dispose(); }
                    }
                }

                EventBus.Instance.Publish(new OptimizationAppliedEvent
                {
                    ActionName = "Process Throttle",
                    Description = $"Lowered priority on {_throttled.Count} background processes."
                });
            });
        }

        /// <summary>Restores all throttled processes to their original priority.</summary>
        public async Task RestoreProcessPrioritiesAsync()
        {
            await Task.Run(() =>
            {
                List<int> toRestore;
                Dictionary<int, ProcessPriorityClass> originals;
                lock (_lock)
                {
                    toRestore = new List<int>(_throttled);
                    originals = new Dictionary<int, ProcessPriorityClass>(_originalPriorities);
                }
                foreach (var pid in toRestore)
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (originals.TryGetValue(pid, out var orig))
                            p.PriorityClass = orig;
                        p.Dispose();
                    }
                    catch { }
                }
                lock (_lock)
                {
                    _throttled.Clear();
                    _originalPriorities.Clear();
                }
            });
        }

        public async Task SetProcessPriorityAsync(int pid, ProcessPriorityClass priority)
        {
            await Task.Run(() =>
            {
                try
                {
                    var p = Process.GetProcessById(pid);
                    if (_neverTouch.Contains(p.ProcessName)) return;
                    lock (_lock)
                    {
                        if (!_originalPriorities.ContainsKey(pid))
                            _originalPriorities[pid] = p.PriorityClass;
                    }
                    p.PriorityClass = priority;
                    p.Dispose();
                }
                catch { }
            });
        }

        private static string GetDescription(Process p)
        {
            try { return p.MainModule?.FileVersionInfo.FileDescription ?? p.ProcessName; }
            catch { return p.ProcessName; }
        }

        private static DateTime TryGetStartTime(Process p)
        {
            try { return p.StartTime; }
            catch { return DateTime.MinValue; }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
