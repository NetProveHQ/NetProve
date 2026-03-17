using System;

namespace NetProve.Models
{
    public enum BrowserType { Chrome, Edge, Firefox, Opera, Yandex }

    public sealed class CacheInfo
    {
        public BrowserType Browser { get; init; }
        public string CachePath { get; init; } = "";
        public long SizeBytes { get; init; }
        public long LimitBytes { get; init; }
        public bool IsInstalled { get; init; }
        public DateTime LastScanned { get; init; } = DateTime.Now;

        public float SizeMb => SizeBytes / 1_048_576f;
        public float LimitMb => LimitBytes / 1_048_576f;
        public float UsagePercent => LimitBytes > 0 ? (float)SizeBytes / LimitBytes * 100f : 0f;
        public bool ExceedsLimit => SizeBytes > LimitBytes;
        public string UsageDisplay => $"{SizeMb:F0} / {LimitMb:F0} MB";

        public string SizeDisplay => SizeBytes >= 1_073_741_824
            ? $"{SizeBytes / 1_073_741_824f:F1} GB"
            : $"{SizeMb:F0} MB";
    }
}
