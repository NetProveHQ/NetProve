using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NetProve.Core;
using NetProve.Models;

namespace NetProve.Managers
{
    /// <summary>
    /// Scans browser cache directories, reports sizes, and safely removes
    /// old/unused cache files when the configured limit is exceeded.
    /// Never removes files that may be active streaming buffers (modified recently).
    /// </summary>
    public sealed class CacheManager
    {
        private static readonly string UserProfile = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);
        private static readonly string LocalAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        private static readonly string AppData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        // ── Browser cache path definitions ───────────────────────────────────
        private static readonly Dictionary<BrowserType, string[]> CachePaths = new()
        {
            [BrowserType.Chrome] = new[]
            {
                Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Default", "Cache"),
                Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Default", "Code Cache"),
                Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Default", "GPUCache")
            },
            [BrowserType.Edge] = new[]
            {
                Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Default", "Cache"),
                Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Default", "Code Cache"),
                Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Default", "GPUCache")
            },
            [BrowserType.Firefox] = new[]
            {
                Path.Combine(LocalAppData, "Mozilla", "Firefox", "Profiles"),
            },
            [BrowserType.Opera] = new[]
            {
                Path.Combine(AppData, "Opera Software", "Opera Stable", "Cache"),
                Path.Combine(AppData, "Opera Software", "Opera GX Stable", "Cache")
            },
            [BrowserType.Yandex] = new[]
            {
                Path.Combine(LocalAppData, "Yandex", "YandexBrowser", "User Data", "Default", "Cache"),
                Path.Combine(LocalAppData, "Yandex", "YandexBrowser", "User Data", "Default", "Code Cache")
            }
        };

        public async Task<List<CacheInfo>> ScanAllAsync()
        {
            var results = new List<CacheInfo>();
            foreach (BrowserType browser in Enum.GetValues<BrowserType>())
            {
                var info = await ScanBrowserAsync(browser);
                results.Add(info);
            }
            return results;
        }

        public async Task<CacheInfo> ScanBrowserAsync(BrowserType browser)
        {
            return await Task.Run(() =>
            {
                long limitBytes = GetLimitBytes(browser);
                long totalSize = 0;
                string firstPath = "";

                if (CachePaths.TryGetValue(browser, out var paths))
                {
                    foreach (var path in paths)
                    {
                        // Firefox has profile sub-dirs
                        if (browser == BrowserType.Firefox && Directory.Exists(path))
                        {
                            foreach (var profile in Directory.GetDirectories(path))
                            {
                                var ffCache = Path.Combine(profile, "cache2");
                                if (Directory.Exists(ffCache))
                                {
                                    if (string.IsNullOrEmpty(firstPath)) firstPath = ffCache;
                                    totalSize += GetDirectorySize(ffCache);
                                }
                            }
                        }
                        else if (Directory.Exists(path))
                        {
                            if (string.IsNullOrEmpty(firstPath)) firstPath = path;
                            totalSize += GetDirectorySize(path);
                        }
                    }
                }

                bool installed = totalSize > 0 || (firstPath.Length > 0 && Directory.Exists(firstPath));

                return new CacheInfo
                {
                    Browser = browser,
                    CachePath = firstPath,
                    SizeBytes = totalSize,
                    LimitBytes = limitBytes,
                    IsInstalled = installed
                };
            });
        }

        /// <summary>
        /// Clears old cache files for a browser.
        /// Protects files modified within the last 30 minutes (active buffers).
        /// Returns bytes freed.
        /// </summary>
        public async Task<long> ClearCacheAsync(BrowserType browser, bool forceAll = false)
        {
            return await Task.Run(() =>
            {
                long freed = 0;
                var cutoff = DateTime.Now.AddMinutes(-30); // protect recent files

                if (!CachePaths.TryGetValue(browser, out var paths)) return 0L;

                foreach (var basePath in paths)
                {
                    var dirs = new List<string>();

                    if (browser == BrowserType.Firefox && Directory.Exists(basePath))
                    {
                        foreach (var profile in Directory.GetDirectories(basePath))
                        {
                            var ffCache = Path.Combine(profile, "cache2");
                            if (Directory.Exists(ffCache)) dirs.Add(ffCache);
                        }
                    }
                    else if (Directory.Exists(basePath))
                        dirs.Add(basePath);

                    foreach (var dir in dirs)
                    {
                        try
                        {
                            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    var fi = new FileInfo(file);
                                    // Skip recently-modified files unless forceAll
                                    if (!forceAll && fi.LastWriteTime > cutoff) continue;
                                    long sz = fi.Length;
                                    fi.Delete();
                                    freed += sz;
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }

                if (freed > 0)
                {
                    EventBus.Instance.Publish(new OptimizationAppliedEvent
                    {
                        ActionName = "Cache Clear",
                        Description = $"Cleared {browser} cache. Freed {freed / 1_048_576f:F0} MB."
                    });
                }
                return freed;
            });
        }

        private static long GetDirectorySize(string path)
        {
            try
            {
                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Sum(f =>
                    {
                        try { return new FileInfo(f).Length; }
                        catch { return 0L; }
                    });
            }
            catch { return 0; }
        }

        private static long GetLimitBytes(BrowserType browser)
        {
            var s = AppSettings.Instance;
            return browser switch
            {
                BrowserType.Chrome => s.ChromeCacheLimitMb * 1_048_576,
                BrowserType.Edge => s.EdgeCacheLimitMb * 1_048_576,
                BrowserType.Firefox => s.FirefoxCacheLimitMb * 1_048_576,
                BrowserType.Opera => s.OperaCacheLimitMb * 1_048_576,
                BrowserType.Yandex => s.YandexCacheLimitMb * 1_048_576,
                _ => 500 * 1_048_576
            };
        }
    }
}
