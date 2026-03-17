using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetProve.Core;
using NetProve.Models;

namespace NetProve.Engines
{
    /// <summary>
    /// Detects when a game from a supported launcher is running.
    /// Uses a minimal-overhead scan of the running process list every few seconds.
    /// </summary>
    public sealed class GameDetector : IDisposable
    {
        // ── Platform launcher processes ───────────────────────────────────────
        private static readonly HashSet<string> LauncherProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "steam","steamwebhelper",        // Steam
            "EpicGamesLauncher",             // Epic
            "RiotClientServices","LeagueClient","RiotClientUx", // Riot
            "Battle.net","BlizzardError",    // Battle.net
            "UbisoftConnect","upc",          // Ubisoft
            "EADesktop","EALauncher","Origin" // EA
        };

        // ── Known game process signatures → (name, platform) ─────────────────
        private static readonly Dictionary<string, (string Name, string Platform)> KnownGames =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // Riot
            ["LeagueOfLegends"] = ("League of Legends", "Riot"),
            ["VALORANT-Win64-Shipping"] = ("VALORANT", "Riot"),
            ["RiotClientCrashHandler"] = ("Riot Game", "Riot"),
            // Steam (common)
            ["csgo"] = ("CS:GO", "Steam"),
            ["cs2"] = ("Counter-Strike 2", "Steam"),
            ["dota2"] = ("Dota 2", "Steam"),
            ["hl2"] = ("Half-Life 2", "Steam"),
            ["RocketLeague"] = ("Rocket League", "Epic/Steam"),
            ["RainbowSix"] = ("Rainbow Six Siege", "Ubisoft"),
            // Battle.net
            ["Overwatch"] = ("Overwatch", "Battle.net"),
            ["Warzone"] = ("Call of Duty: Warzone", "Battle.net"),
            ["Diablo IV"] = ("Diablo IV", "Battle.net"),
            // EA
            ["FIFA24"] = ("FIFA 24", "EA"),
            ["bf2042"] = ("Battlefield 2042", "EA"),
            // Epic
            ["FortniteClient-Win64-Shipping"] = ("Fortnite", "Epic"),
            // Generic patterns handled by heuristic below
        };

        // ── Heuristic keywords that suggest a game process ───────────────────
        private static readonly string[] GameKeywords =
            { "game", "engine", "launcher", "shipping", "win64", "play" };

        private GameSession? _currentSession;
        private CancellationTokenSource? _cts;
        private Task? _task;

        public GameSession? CurrentSession => _currentSession;
        public bool IsGameRunning => _currentSession?.IsActive == true;

        public void Start(CancellationToken externalToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _task = Task.Run(() => DetectLoop(_cts.Token), _cts.Token);
        }

        public void Stop() { _cts?.Cancel(); try { _task?.Wait(2000); } catch { } }

        private async Task DetectLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(4000, ct);
                    ScanProcesses();
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }

        private void ScanProcesses()
        {
            var procs = Process.GetProcesses();
            bool foundGame = false;
            string gameName = "";
            string platform = "";
            int gamePid = 0;

            foreach (var p in procs)
            {
                try
                {
                    // Exact match
                    if (KnownGames.TryGetValue(p.ProcessName, out var info))
                    {
                        foundGame = true;
                        gameName = info.Name;
                        platform = info.Platform;
                        gamePid = p.Id;
                        break;
                    }

                    // Launcher-spawned process heuristic: parent is a launcher
                    if (IsLikelyGameProcess(p))
                    {
                        foundGame = true;
                        gameName = p.ProcessName;
                        platform = InferPlatform(p);
                        gamePid = p.Id;
                        break;
                    }
                }
                catch { }
                finally { p.Dispose(); }
            }

            if (foundGame && (_currentSession == null || !_currentSession.IsActive))
            {
                // Game started
                _currentSession = new GameSession
                {
                    GameName = gameName,
                    Platform = platform,
                    ProcessId = gamePid
                };
                EventBus.Instance.Publish(new GameDetectedEvent
                {
                    GameName = gameName,
                    Platform = platform,
                    ProcessId = gamePid
                });
            }
            else if (!foundGame && _currentSession?.IsActive == true)
            {
                // Game ended
                _currentSession.EndTime = DateTime.Now;
                EventBus.Instance.Publish(new GameEndedEvent
                {
                    GameName = _currentSession.GameName,
                    SessionEnd = _currentSession.EndTime.Value
                });
            }
        }

        private static bool IsLikelyGameProcess(Process p)
        {
            try
            {
                var path = p.MainModule?.FileName ?? "";
                // High memory + no window title isn't enough – also check path keywords
                bool hasGamePath = GameKeywords.Any(k =>
                    path.Contains(k, StringComparison.OrdinalIgnoreCase));
                bool isLauncherChild = IsChildOfLauncher(p);
                return (hasGamePath || isLauncherChild) && p.WorkingSet64 > 200 * 1024 * 1024;
            }
            catch { return false; }
        }

        private static bool IsChildOfLauncher(Process p)
        {
            try
            {
                // Walk parent chain via WMI (lightweight – single query)
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId={p.Id}");
                foreach (var obj in searcher.Get())
                {
                    int parentId = Convert.ToInt32(obj["ParentProcessId"]);
                    try
                    {
                        using var parent = Process.GetProcessById(parentId);
                        if (LauncherProcesses.Contains(parent.ProcessName)) return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        private static string InferPlatform(Process p)
        {
            try
            {
                var path = p.MainModule?.FileName ?? "";
                if (path.Contains("Steam", StringComparison.OrdinalIgnoreCase)) return "Steam";
                if (path.Contains("Epic", StringComparison.OrdinalIgnoreCase)) return "Epic Games";
                if (path.Contains("Riot", StringComparison.OrdinalIgnoreCase)) return "Riot";
                if (path.Contains("Battle.net", StringComparison.OrdinalIgnoreCase)) return "Battle.net";
                if (path.Contains("Ubisoft", StringComparison.OrdinalIgnoreCase)) return "Ubisoft";
                if (path.Contains("EA", StringComparison.OrdinalIgnoreCase)) return "EA";
            }
            catch { }
            return "Unknown";
        }

        public void Dispose() { Stop(); _cts?.Dispose(); }
    }
}
