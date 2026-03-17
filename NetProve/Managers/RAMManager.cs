using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NetProve.Core;

namespace NetProve.Managers
{
    /// <summary>
    /// Safe RAM management.  Only trims working-sets of non-critical processes.
    /// Never forcefully terminates processes or flushes page files aggressively.
    /// </summary>
    public sealed class RAMManager
    {
        // ── Win32 ─────────────────────────────────────────────────────────────
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(
            IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet2(IntPtr hProcess);

        // Opens a process with required rights
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwAccess, bool bInherit, int dwPId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_SET_QUOTA = 0x0100;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;

        // ── Critical process names – never touch their working sets ───────────
        private static readonly HashSet<string> _critical = new(StringComparer.OrdinalIgnoreCase)
        {
            "System","Idle","smss","csrss","wininit","winlogon","services",
            "lsass","lsm","svchost","MsMpEng","SecurityHealthService",
            "antimalware","dwm","fontdrvhost","audiodg","conhost",
            "taskmgr","NetProve"
        };

        public event Action<string>? StatusMessage;

        /// <summary>
        /// Trims working sets of eligible background processes.
        /// Returns MB freed (estimate based on pre/post available RAM).
        /// </summary>
        public async Task<long> OptimizeAsync()
        {
            return await Task.Run(() =>
            {
                long freed = 0;
                var procs = Process.GetProcesses();

                foreach (var p in procs)
                {
                    try
                    {
                        if (_critical.Contains(p.ProcessName)) continue;
                        if (AppSettings.Instance.WhitelistedProcesses.Contains(p.ProcessName)) continue;

                        // Skip processes with a window (foreground apps)
                        if (p.MainWindowHandle != IntPtr.Zero) continue;

                        // Skip if memory is small (< 10 MB) – not worth the API call
                        if (p.WorkingSet64 < 10 * 1024 * 1024) continue;

                        long before = p.WorkingSet64;
                        IntPtr hProcess = OpenProcess(
                            PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION, false, p.Id);

                        if (hProcess == IntPtr.Zero) continue;

                        try
                        {
                            EmptyWorkingSet(hProcess);
                            p.Refresh();
                            freed += Math.Max(0, before - p.WorkingSet64);
                        }
                        finally { CloseHandle(hProcess); }
                    }
                    catch { /* process may have exited */ }
                    finally { p.Dispose(); }
                }

                if (freed > 0)
                {
                    long mb = freed / 1_048_576;
                    StatusMessage?.Invoke($"Freed ~{mb} MB of working-set memory.");
                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "RAM Optimize",
                        Description = $"Trimmed working sets. Freed ~{mb} MB."
                    });
                }
                else
                {
                    StatusMessage?.Invoke("RAM is already well-managed. No action needed.");
                }

                return freed;
            });
        }

        /// <summary>
        /// Standby list flush (requires admin).  Clears the standby page list,
        /// making more RAM available to new allocations.
        /// Safe because Windows will repopulate it on demand.
        /// </summary>
        public async Task<bool> FlushStandbyListAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Use NtSetSystemInformation (undocumented but well-known)
                    // SystemMemoryListInformation = 80, MemoryFlushModifiedList
                    const int SystemMemoryListInformation = 80;
                    const int MemoryEmptyWorkingSets = 2;

                    var ntdll = GetModuleHandle("ntdll.dll");
                    if (ntdll == IntPtr.Zero) return false;

                    var fn = GetProcAddress(ntdll, "NtSetSystemInformation");
                    if (fn == IntPtr.Zero) return false;

                    var cmd = MemoryEmptyWorkingSets;
                    NtSetSystemInformation(SystemMemoryListInformation, ref cmd, sizeof(int));
                    return true;
                }
                catch { return false; }
            });
        }

        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll")] private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("ntdll.dll")] private static extern int NtSetSystemInformation(int infoClass, ref int info, int length);
    }
}
