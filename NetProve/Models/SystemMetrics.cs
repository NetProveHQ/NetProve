using System;

namespace NetProve.Models
{
    public sealed class SystemMetrics
    {
        public DateTime Timestamp { get; init; } = DateTime.Now;

        // CPU
        public float CpuUsagePercent { get; init; }
        public int CpuCores { get; init; }
        public string CpuName { get; init; } = "";

        // RAM
        public long TotalRamBytes { get; init; }
        public long AvailableRamBytes { get; init; }
        public long UsedRamBytes => TotalRamBytes - AvailableRamBytes;
        public float RamUsagePercent => TotalRamBytes > 0
            ? (float)UsedRamBytes / TotalRamBytes * 100f
            : 0f;
        public float TotalRamGb => TotalRamBytes / 1_073_741_824f;
        public float UsedRamGb => UsedRamBytes / 1_073_741_824f;
        public float AvailableRamGb => AvailableRamBytes / 1_073_741_824f;

        // Disk
        public float DiskReadBytesPerSec { get; init; }
        public float DiskWriteBytesPerSec { get; init; }
        public float DiskActivityPercent { get; init; }

        // Summary helpers
        public float DiskTotalMbPerSec =>
            (DiskReadBytesPerSec + DiskWriteBytesPerSec) / 1_048_576f;
    }
}
