# NetProve — Windows Performance Optimizer

A professional, lightweight Windows desktop optimization tool designed to maintain stable
performance during long gaming sessions and video streaming.

---

## Quick Start

### Requirements
- Windows 10 / Windows 11 (x64)
- .NET 8 SDK → https://dotnet.microsoft.com/download/dotnet/8.0
- Visual Studio 2022+ **or** the .NET CLI (dotnet build)
- **Run as Administrator** (required for RAM management and TCP tuning)

### Build & Run
```powershell
# Clone / navigate to the project folder
cd C:\Users\ASUS\NetPulse

# Restore NuGet packages
dotnet restore NetProve\NetProve.csproj

# Build release
dotnet build NetProve\NetProve.csproj -c Release

# Run
dotnet run --project NetProve\NetProve.csproj
```

Or open `NetProve.sln` in Visual Studio 2022 and press **F5**.

---

## Architecture

```
NetProve/
├── Core/
│   ├── EventBus.cs          — Lightweight publish/subscribe message bus
│   ├── CoreEngine.cs        — Central orchestrator (singleton)
│   └── AppSettings.cs       — Persistent user configuration
│
├── Models/
│   ├── SystemMetrics.cs     — CPU / RAM / Disk snapshot
│   ├── NetworkMetrics.cs    — Ping / jitter / packet-loss / throughput
│   ├── ProcessInfo.cs       — Process resource usage model
│   ├── CacheInfo.cs         — Browser cache size model
│   ├── LagAnalysisResult.cs — Root-cause analysis result
│   ├── GameSession.cs       — Active gaming session data
│   └── PerformanceReport.cs — Post-session performance report
│
├── Monitors/
│   ├── SystemMonitor.cs     — CPU, RAM, Disk via PerformanceCounter + Win32
│   └── NetworkAnalyzer.cs   — Ping, jitter, loss, NIC throughput
│
├── Managers/
│   ├── RAMManager.cs        — Safe working-set trimming (EmptyWorkingSet)
│   ├── ProcessManager.cs    — Priority management (never terminates)
│   └── CacheManager.cs      — Browser cache scanning and safe cleanup
│
├── Engines/
│   ├── GameDetector.cs      — Detects games from 6 major launchers
│   ├── LagAnalysisEngine.cs — Root-cause analysis of lag
│   ├── LagPredictionEngine.cs — Early warning via trend analysis
│   ├── OptimizationEngine.cs  — Rule-based optimization triggers
│   ├── NetworkOptimizer.cs  — DNS flush, TCP tuning (safe/reversible)
│   ├── SpeedTestEngine.cs   — Download/upload speed test via CDN
│   └── PerformanceReportEngine.cs — Post-session reports
│
├── Controls/
│   ├── CircularGauge.cs     — Custom WPF arc gauge
│   └── MiniLineChart.cs     — Custom real-time line chart
│
├── ViewModels/
│   ├── BaseViewModel.cs     — INotifyPropertyChanged base
│   └── MainViewModel.cs     — Full application state + commands
│
├── Converters/
│   └── ValueConverters.cs   — WPF value converters (color, text)
│
├── MainWindow.xaml          — Complete WPF UI
└── MainWindow.xaml.cs       — UI code-behind
```

### Module Communication
All modules communicate through `EventBus` (publish/subscribe).
No module has direct references to the UI layer.

```
SystemMonitor  ──publish──► SystemMetricsUpdatedEvent ──► MainViewModel
NetworkAnalyzer ─publish──► NetworkMetricsUpdatedEvent ──► MainViewModel
GameDetector   ──publish──► GameDetectedEvent ──► OptimizationEngine → Gaming Mode
LagPrediction  ──publish──► LagWarningEvent ──► UI Banner
```

---

## Features

| Module | What it does |
|---|---|
| Dashboard | Live CPU, RAM, Disk, Network gauges + mini charts |
| System Monitor | Real-time metrics from Windows PerformanceCounters |
| RAM Manager | Safe working-set trim of background processes |
| Process Manager | View & throttle background processes during gaming |
| Cache Manager | Scan & clean Chrome, Edge, Firefox, Opera, Yandex caches |
| Game Detector | Auto-detects Steam, Epic, Riot, Battle.net, Ubisoft, EA games |
| Network Analyzer | Ping / jitter / packet-loss measurement every 3 s |
| Network Optimizer | DNS flush, TCP stack tuning (reversible) |
| Speed Test | Real download/upload test via Cloudflare CDN |
| Lag Analysis | Root-cause analysis: CPU / RAM / Disk / Network / Background |
| Lag Prediction | Trend-based early warning before lag occurs |
| Performance Reports | Automatic post-session report with rating + suggestions |

---

## Safety Rules

NetProve **never**:
- Modifies critical Windows settings permanently
- Terminates system processes
- Intercepts or modifies network packets
- Injects into game memory
- Interferes with anti-cheat systems

All optimizations are **reversible** on demand.

---

## Performance Targets

| Metric | Target | How achieved |
|---|---|---|
| Idle CPU | < 1% | Event-driven; 2–5 s polling intervals |
| RAM usage | < 100 MB | No heavyweight libraries; lean models |
| UI thread | Non-blocking | All I/O on background Task threads |
| Network | No packet modification | Only ping + HTTP; no raw sockets |

---

## Performance Testing Plan

1. **Idle overhead**: Run for 10 min with no game — check CPU < 1%, RAM < 100 MB.
2. **Gauge accuracy**: Compare CPU% with Task Manager; delta < 3%.
3. **Network accuracy**: Compare ping with `ping 8.8.8.8` — delta < 5 ms.
4. **RAM optimization**: Measure available RAM before/after with Task Manager.
5. **Game detection**: Launch Steam game — verify Gaming Mode activates within 8 s.
6. **Speed test**: Compare result with https://fast.com — delta < 20%.
7. **Cache scan**: Verify reported sizes match browser's own cache info pages.
8. **Lag analysis**: Simulate high CPU (stress test) — verify CPU Bottleneck detected.

---

## Future Feature Suggestions

1. **GPU monitoring** — NVIDIA/AMD GPU usage via NVML/ADL.
2. **Frame-time overlay** — HUD showing FPS and frame-time spikes.
3. **Wi-Fi channel analyser** — Detect congested channels.
4. **Startup manager** — Disable high-impact startup entries.
5. **Cloud sync** — Back up settings and reports to OneDrive/Dropbox.
6. **Scheduled optimizations** — Clean cache and optimize RAM on a timer.
7. **Per-game profiles** — Custom settings for each detected game.
8. **Tray integration** — Minimize to system tray with quick-action menu.
9. **Dark/Light theme toggle** — User selectable in settings.
10. **Notification center** — Aggregated lag warnings and reports.
