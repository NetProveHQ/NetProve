using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NetProve.Localization;
using NetProve.Models;

namespace NetProve.Engines
{
    /// <summary>
    /// Performs download and upload speed tests using publicly accessible endpoints.
    /// Uses a progress callback so the UI can show live progress.
    /// </summary>
    public sealed class SpeedTestEngine
    {
        // ── Test endpoints ────────────────────────────────────────────────────
        // Cloudflare & fast.com CDN files (public, no auth required)
        private static readonly string[] DownloadUrls =
        {
            "https://speed.cloudflare.com/__down?bytes=25000000",   // 25 MB Cloudflare
            "https://proof.ovh.net/files/10Mb.dat",                 // 10 MB OVH
            "https://speedtest.tele2.net/10MB.zip",                 // 10 MB Tele2
        };

        private static readonly string UploadUrl = "https://speed.cloudflare.com/__up";

        private readonly HttpClient _http;

        public SpeedTestEngine()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            _http.DefaultRequestHeaders.Add("User-Agent", "NetProve/1.0 SpeedTest");
        }

        /// <summary>
        /// Runs a full speed test: ping, download, upload.
        /// </summary>
        public async Task<SpeedTestResult> RunAsync(
            IProgress<SpeedTestProgress>? progress = null,
            CancellationToken ct = default)
        {
            var loc = LocalizationManager.Instance;
            progress?.Report(new SpeedTestProgress { Stage = loc["MeasuringPing"], Percent = 5 });

            double pingMs = await MeasurePingAsync(ct);

            progress?.Report(new SpeedTestProgress { Stage = loc["TestingDownload"], Percent = 15 });
            double dlMbps = await MeasureDownloadSpeedAsync(progress, ct);

            progress?.Report(new SpeedTestProgress { Stage = loc["TestingUpload"], Percent = 75 });
            double ulMbps = await MeasureUploadSpeedAsync(progress, ct);

            progress?.Report(new SpeedTestProgress { Stage = loc["SpeedTestComplete"], Percent = 100 });

            return new SpeedTestResult
            {
                DownloadMbps = Math.Round(dlMbps, 2),
                UploadMbps = Math.Round(ulMbps, 2),
                PingMs = Math.Round(pingMs, 1),
                Server = "Cloudflare CDN",
                Success = dlMbps > 0
            };
        }

        private async Task<double> MeasurePingAsync(CancellationToken ct)
        {
            double total = 0;
            int success = 0;
            for (int i = 0; i < 5; i++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync("1.1.1.1", 2000);
                    if (reply.Status == IPStatus.Success)
                    {
                        total += reply.RoundtripTime;
                        success++;
                    }
                }
                catch { }
                await Task.Delay(100, ct);
            }
            return success > 0 ? total / success : 999;
        }

        private async Task<double> MeasureDownloadSpeedAsync(
            IProgress<SpeedTestProgress>? progress, CancellationToken ct)
        {
            foreach (var url in DownloadUrls)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var sw = Stopwatch.StartNew();
                    long bytes = 0;
                    using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                    response.EnsureSuccessStatusCode();

                    long? total = response.Content.Headers.ContentLength;
                    using var stream = await response.Content.ReadAsStreamAsync(ct);
                    var buffer = new byte[81920];
                    int read;

                    while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                    {
                        bytes += read;
                        if (total > 0)
                        {
                            int pct = (int)(15 + bytes * 55 / total.Value);
                            progress?.Report(new SpeedTestProgress
                            {
                                Stage = $"Downloading… {bytes / 1_048_576f:F1} MB",
                                Percent = pct,
                                CurrentMbps = bytes * 8.0 / sw.Elapsed.TotalSeconds / 1_000_000
                            });
                        }
                    }

                    sw.Stop();
                    if (bytes > 0 && sw.Elapsed.TotalSeconds > 0)
                        return bytes * 8.0 / sw.Elapsed.TotalSeconds / 1_000_000;
                }
                catch (OperationCanceledException) { throw; }
                catch { /* try next URL */ }
            }
            return 0;
        }

        private async Task<double> MeasureUploadSpeedAsync(
            IProgress<SpeedTestProgress>? progress, CancellationToken ct)
        {
            try
            {
                // Upload 5 MB of random data
                const int uploadSize = 5 * 1024 * 1024;
                var data = new byte[uploadSize];
                new Random().NextBytes(data);

                var sw = Stopwatch.StartNew();
                using var content = new ByteArrayContent(data);

                progress?.Report(new SpeedTestProgress
                {
                    Stage = "Uploading data…",
                    Percent = 80
                });

                await _http.PostAsync(UploadUrl, content, ct);
                sw.Stop();

                if (sw.Elapsed.TotalSeconds > 0)
                    return uploadSize * 8.0 / sw.Elapsed.TotalSeconds / 1_000_000;
            }
            catch (OperationCanceledException) { throw; }
            catch { }
            return 0;
        }
    }

    public sealed class SpeedTestProgress
    {
        public string Stage { get; init; } = "";
        public int Percent { get; init; }
        public double CurrentMbps { get; init; }
    }
}
