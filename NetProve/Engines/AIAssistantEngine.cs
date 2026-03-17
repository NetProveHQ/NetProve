using System;
using System.Collections.Generic;
using System.Linq;
using NetProve.Core;
using NetProve.Localization;

namespace NetProve.Engines
{
    /// <summary>
    /// Local knowledge-base chatbot for NetProve.
    /// Pattern-matching with context-aware responses. No API key required.
    /// </summary>
    public sealed class AIAssistantEngine
    {
        private readonly List<ChatRule> _rules;

        public AIAssistantEngine()
        {
            _rules = BuildRules();
        }

        /// <summary>Gets a localized welcome greeting for when the Assistant page opens.</summary>
        public string GetWelcomeMessage() => GetLocalizedResponse("welcome");

        /// <summary>Get a response for user input based on keyword matching.</summary>
        public string GetResponse(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return GetLocalizedResponse("empty");

            var normalized = userInput.ToLowerInvariant().Trim();
            var words = normalized.Split(new[] { ' ', ',', '.', '?', '!', ':', ';' },
                StringSplitOptions.RemoveEmptyEntries);

            // Score each rule
            var scored = _rules.Select(r => new
            {
                Rule = r,
                Score = r.Keywords.Count(kw => words.Any(w => w.Contains(kw)) ||
                                                normalized.Contains(kw))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Rule.Priority)
            .ToList();

            if (scored.Count > 0 && scored[0].Score >= 1)
                return scored[0].Rule.ResponseGenerator();

            return GetLocalizedResponse("nomatch");
        }

        private List<ChatRule> BuildRules()
        {
            return new List<ChatRule>
            {
                // ── Greeting ──
                new(new[] { "hello", "hi", "hey", "merhaba", "selam", "hola", "你好", "こんにちは" },
                    "General", 10,
                    () => GetLocalizedResponse("greeting")),

                // ── What can you do ──
                new(new[] { "help", "what", "can", "yardım", "ne", "yapabilir", "ayuda", "帮助", "ヘルプ" },
                    "General", 9,
                    () => GetLocalizedResponse("capabilities")),

                // ── RAM / Memory ──
                new(new[] { "ram", "memory", "bellek", "hafıza", "memoria", "内存", "メモリ" },
                    "RAM", 8,
                    () => {
                        try
                        {
                            var metrics = CoreEngine.Instance.SystemMonitor.Latest;
                            float ramPct = metrics?.RamUsagePercent ?? 0;
                            return ramPct > 80
                                ? GetLocalizedResponse("ram_high").Replace("{pct}", $"{ramPct:F0}")
                                : GetLocalizedResponse("ram_ok").Replace("{pct}", $"{ramPct:F0}");
                        }
                        catch { return GetLocalizedResponse("ram_generic"); }
                    }),

                // ── CPU ──
                new(new[] { "cpu", "processor", "işlemci", "procesador", "处理器", "プロセッサ" },
                    "CPU", 8,
                    () => {
                        try
                        {
                            var metrics = CoreEngine.Instance.SystemMonitor.Latest;
                            float cpuPct = metrics?.CpuUsagePercent ?? 0;
                            return cpuPct > 80
                                ? GetLocalizedResponse("cpu_high").Replace("{pct}", $"{cpuPct:F0}")
                                : GetLocalizedResponse("cpu_ok").Replace("{pct}", $"{cpuPct:F0}");
                        }
                        catch { return GetLocalizedResponse("cpu_generic"); }
                    }),

                // ── Ping / Latency ──
                new(new[] { "ping", "latency", "gecikme", "latencia", "延迟", "レイテンシ" },
                    "Network", 8,
                    () => {
                        try
                        {
                            var net = CoreEngine.Instance.NetworkAnalyzer.Latest;
                            double ping = net?.PingMs ?? 0;
                            return ping > 100
                                ? GetLocalizedResponse("ping_high").Replace("{ms}", $"{ping:F0}")
                                : GetLocalizedResponse("ping_ok").Replace("{ms}", $"{ping:F0}");
                        }
                        catch { return GetLocalizedResponse("ping_generic"); }
                    }),

                // ── Lag ──
                new(new[] { "lag", "kasma", "stutter", "freeze", "donma", "卡顿", "ラグ" },
                    "Lag", 8,
                    () => GetLocalizedResponse("lag_help")),

                // ── Gaming ──
                new(new[] { "game", "gaming", "oyun", "juego", "游戏", "ゲーム" },
                    "Gaming", 7,
                    () => GetLocalizedResponse("gaming_tips")),

                // ── DNS ──
                new(new[] { "dns", "domain", "alan adı" },
                    "Network", 7,
                    () => GetLocalizedResponse("dns_help")),

                // ── Speed / Bandwidth ──
                new(new[] { "speed", "bandwidth", "hız", "bant", "velocidad", "速度", "速い" },
                    "Network", 7,
                    () => GetLocalizedResponse("speed_help")),

                // ── Cache ──
                new(new[] { "cache", "önbellek", "browser", "tarayıcı", "caché", "缓存", "キャッシュ" },
                    "Cache", 7,
                    () => GetLocalizedResponse("cache_help")),

                // ── Optimize ──
                new(new[] { "optimize", "optimiz", "improve", "iyileştir", "boost", "geliştir", "优化", "最適化" },
                    "Optimization", 7,
                    () => GetLocalizedResponse("optimize_help")),

                // ── Auto mode ──
                new(new[] { "auto", "automatic", "otomatik", "automático", "自动", "自動" },
                    "AutoMode", 7,
                    () => GetLocalizedResponse("auto_mode_help")),

                // ── Streaming ──
                new(new[] { "stream", "yayın", "obs", "twitch", "broadcast", "transmisión", "直播", "配信" },
                    "Streaming", 6,
                    () => GetLocalizedResponse("streaming_tips")),

                // ── Network reset ──
                new(new[] { "reset", "sıfırla", "winsock", "restablecer", "重置", "リセット" },
                    "Network", 6,
                    () => GetLocalizedResponse("network_reset_help")),

                // ── Nagle ──
                new(new[] { "nagle", "tcpnodelay", "ackfrequency" },
                    "Network", 6,
                    () => GetLocalizedResponse("nagle_help")),

                // ── Power plan ──
                new(new[] { "power", "güç", "plan", "energy", "enerji", "电源", "電源" },
                    "Gaming", 6,
                    () => GetLocalizedResponse("power_plan_help")),

                // ── Packet loss ──
                new(new[] { "packet", "loss", "paket", "kaybı", "pérdida", "丢包", "パケット" },
                    "Network", 7,
                    () => GetLocalizedResponse("packet_loss_help")),

                // ── Jitter ──
                new(new[] { "jitter", "titreşim", "fluctuation" },
                    "Network", 6,
                    () => GetLocalizedResponse("jitter_help")),

                // ── WiFi ──
                new(new[] { "wifi", "wi-fi", "wireless", "kablosuz", "inalámbrico", "无线", "ワイヤレス" },
                    "Network", 6,
                    () => GetLocalizedResponse("wifi_help")),

                // ── Thanks ──
                new(new[] { "thanks", "thank", "teşekkür", "sağol", "gracias", "谢谢", "ありがとう" },
                    "General", 5,
                    () => GetLocalizedResponse("thanks")),
            };
        }

        private string GetLocalizedResponse(string key)
        {
            var lang = LocalizationManager.Instance.Current;
            if (_responses.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var resp))
                return resp;
            if (_responses[AppLanguage.English].TryGetValue(key, out var en))
                return en;
            return "I'm not sure how to help with that. Try asking about RAM, CPU, ping, gaming, or optimization.";
        }

        private readonly Dictionary<AppLanguage, Dictionary<string, string>> _responses = new()
        {
            [AppLanguage.English] = new()
            {
                ["welcome"] = "Hey there! 👋 I'm your NetProve assistant. I'm here to help you get the best performance out of your system.\n\nYou can ask me anything — whether it's about your RAM usage, network issues, gaming optimizations, or how to reduce lag. I'll check your system in real-time and give you personalized advice.\n\nGo ahead, type your question below!",
                ["greeting"] = "Hey! 👋 Good to see you! I'm here to help with anything performance-related. Whether it's lag, slow internet, high CPU, or gaming tips — just ask me and I'll take a look at your system right away!",
                ["capabilities"] = "Sure thing! Here's what I can do for you:\n\n🖥 System — Monitor your RAM & CPU, optimize when needed\n🌐 Network — Fix ping issues, benchmark DNS, optimize TCP\n🎮 Gaming — Gaming mode, Nagle toggle, power plan tweaks\n🔍 Diagnostics — Lag analysis, speed tests, Wi-Fi band check\n🤖 Auto Mode — Set it and forget it!\n\nJust tell me what's going on and I'll guide you step by step.",
                ["ram_high"] = "⚠️ Your RAM usage is {pct}% which is high!\n\nRecommendations:\n1. Go to Dashboard → click 'Optimize RAM'\n2. Check Processes page for memory-hungry apps\n3. Enable Auto Mode for automatic optimization\n4. Close unused browser tabs",
                ["ram_ok"] = "✅ Your RAM usage is {pct}% which is healthy.\n\nNo immediate action needed. If you want to free more memory, go to Dashboard → 'Optimize RAM'.",
                ["ram_generic"] = "💡 RAM Optimization Tips:\n1. Click 'Optimize RAM' on the Dashboard\n2. Close unused applications\n3. Enable Auto Mode for automatic management\n4. Check the Processes page for heavy apps",
                ["cpu_high"] = "⚠️ CPU usage is {pct}% — quite high!\n\nRecommendations:\n1. Check Processes page for CPU-heavy apps\n2. Enable Gaming Mode to throttle background processes\n3. Enable Auto Mode for automatic management\n4. Consider closing unnecessary programs",
                ["cpu_ok"] = "✅ CPU usage is {pct}% — looking good.\n\nYour processor is running within normal range.",
                ["cpu_generic"] = "💡 CPU Tips:\n1. Enable Gaming Mode to prioritize games\n2. Auto Mode throttles background processes automatically\n3. Check Processes page to see what's using CPU",
                ["ping_high"] = "⚠️ Your ping is {ms}ms — this may cause lag!\n\nRecommendations:\n1. Flush DNS (Network page)\n2. Run DNS Benchmark to find faster DNS\n3. Use ethernet instead of Wi-Fi\n4. Apply TCP optimizations\n5. Close bandwidth-heavy applications",
                ["ping_ok"] = "✅ Your ping is {ms}ms — excellent!\n\nYour connection latency is good for gaming and streaming.",
                ["ping_generic"] = "💡 Ping Optimization:\n1. Flush DNS cache\n2. Run DNS Benchmark\n3. Apply TCP optimizations\n4. Prefer 5GHz Wi-Fi or ethernet\n5. Close downloads/streams on other devices",
                ["lag_help"] = "🔍 Lag Troubleshooting:\n1. Go to Lag Analysis page → 'Run Analysis'\n2. Check your ping and packet loss on Dashboard\n3. Enable Gaming Mode for priority boost\n4. Flush DNS and apply TCP optimizations\n5. Enable Auto Mode for continuous optimization\n6. Check if your ISP is having issues",
                ["gaming_tips"] = "🎮 Gaming Performance Tips:\n1. Enable Gaming Mode (Dashboard or Gaming page)\n2. Enable Auto Mode — it automatically:\n   • Switches to High Performance power plan\n   • Disables Nagle algorithm\n   • Reduces visual effects\n   • Optimizes RAM\n3. Apply TCP optimizations\n4. Use ethernet over Wi-Fi\n5. Close unnecessary background apps",
                ["dns_help"] = "🌐 DNS Optimization:\n1. Go to Network page → 'DNS Benchmark'\n2. Test Google, Cloudflare, OpenDNS, Quad9\n3. Apply the fastest DNS server\n4. Flush DNS cache after changes\n\nFaster DNS = faster website loading and game server connections.",
                ["speed_help"] = "⚡ Speed Test:\n1. Go to Speed Test page → 'Run Speed Test'\n2. Results show download, upload, and ping\n3. Run DNS Benchmark for better routing\n4. Apply TCP optimizations\n5. Check for bandwidth-heavy apps in Processes",
                ["cache_help"] = "🗑️ Cache Management:\n1. Go to Cache page → 'Scan' to find browser caches\n2. Clear individual browser caches as needed\n3. This frees disk space and can fix browsing issues\n4. Safe operation — only removes temporary files",
                ["optimize_help"] = "⚡ Optimization Guide:\n1. Quick: Click 'Start Optimization' on Dashboard\n2. Network: Flush DNS + Apply TCP optimizations\n3. Gaming: Enable Gaming Mode + Auto Mode\n4. Full: Run DNS Benchmark + Apply fastest DNS\n5. Advanced: Disable Nagle algorithm on Gaming page\n\nAuto Mode handles everything automatically!",
                ["auto_mode_help"] = "🤖 Auto Mode:\nWhen enabled, NetProve automatically:\n• Optimizes RAM when usage exceeds 85%\n• Throttles background processes during gaming\n• Flushes DNS every 30 minutes\n• Switches power plan for gaming\n• Disables Nagle algorithm for lower latency\n\nToggle it from the Dashboard. It's safe and non-intrusive!",
                ["streaming_tips"] = "📡 Streaming Tips:\n1. Enable Streaming Mode (Gaming page)\n2. Apply TCP optimizations\n3. Close unnecessary background apps\n4. Use ethernet over Wi-Fi\n5. Run Speed Test to check upload speed\n6. Enable Auto Mode for automatic optimization",
                ["network_reset_help"] = "🔄 Network Reset:\nGo to Network page → 'Reset Network Stack'\nThis runs:\n• netsh winsock reset\n• netsh int ip reset\n• ipconfig /release + /renew + /flushdns\n\n⚠️ A system restart is recommended after reset.",
                ["nagle_help"] = "📡 Nagle Algorithm:\nDisabling Nagle sends packets immediately instead of buffering.\nThis reduces latency for gaming by 5-15ms.\n\nToggle it on the Gaming page.\nAuto Mode handles this automatically during games.",
                ["power_plan_help"] = "⚡ Power Plan:\nHigh Performance mode prevents CPU throttling.\nAuto Mode switches to it automatically during games.\n\nManual: Gaming page → Power Plan options\nAuto Mode restores your original plan when gaming ends.",
                ["packet_loss_help"] = "📦 Packet Loss Solutions:\n1. Check cable connections\n2. Restart your router\n3. Use ethernet instead of Wi-Fi\n4. Flush DNS cache\n5. Run network reset if persistent\n6. Contact your ISP if loss exceeds 5%",
                ["jitter_help"] = "📊 Jitter Solutions:\nHigh jitter = inconsistent ping = stuttering.\n1. Use ethernet connection\n2. Close bandwidth-heavy apps\n3. Flush DNS cache\n4. Apply TCP optimizations\n5. Switch to 5GHz Wi-Fi band",
                ["wifi_help"] = "📶 Wi-Fi Tips:\n1. Check your band on Network page (2.4GHz vs 5GHz)\n2. 5GHz is faster but shorter range\n3. 2.4GHz has more interference\n4. Position closer to router\n5. For gaming: always prefer ethernet cable",
                ["thanks"] = "Happy to help! 😊 If anything else comes up, I'm right here. Good luck out there!",
                ["nomatch"] = "Hmm, I'm not quite sure about that one. 🤔 But I'm great with these topics:\n\n• RAM or CPU performance\n• Ping, lag, or network issues\n• Gaming & streaming optimization\n• DNS speed and cache management\n• Auto mode setup\n\nTry rephrasing your question and I'll do my best!",
                ["empty"] = "I'm all ears! 💬 Just type your question below and I'll help you out. I can assist with system optimization, network issues, gaming tips, and more.",
            },
            [AppLanguage.Turkish] = new()
            {
                ["welcome"] = "Merhaba! 👋 Ben NetProve asistanınım. Sisteminden en iyi performansı almana yardımcı olmak için buradayım.\n\nRAM kullanımından ağ sorunlarına, oyun optimizasyonundan gecikme azaltmaya kadar her konuda bana sorabilirsin. Sistemini anlık olarak kontrol edip sana özel öneriler vereceğim.\n\nHaydi, aşağıya sorununu yaz!",
                ["greeting"] = "Selam! 👋 Tekrar hoş geldin! Performansla ilgili her konuda yanındayım. Gecikme, yavaş internet, yüksek CPU ya da oyun ipuçları — ne sorarsan sor, hemen sistemine bir göz atayım!",
                ["capabilities"] = "Tabii! İşte sana yardımcı olabileceğim konular:\n\n🖥 Sistem — RAM ve CPU takibi, gerektiğinde optimizasyon\n🌐 Ağ — Ping sorunları, DNS benchmark, TCP optimizasyonu\n🎮 Oyun — Oyun modu, Nagle ayarı, güç planı\n🔍 Teşhis — Gecikme analizi, hız testi, Wi-Fi band kontrolü\n🤖 Otomatik Mod — Kur ve unut!\n\nSorununu anlat, adım adım rehberlik edeyim.",
                ["ram_high"] = "⚠️ RAM kullanımınız %{pct} — yüksek!\n\nÖneriler:\n1. Dashboard → 'RAM Optimize Et' tıklayın\n2. İşlemler sayfasında bellek yiyen uygulamaları kontrol edin\n3. Otomatik Modu etkinleştirin\n4. Kullanılmayan tarayıcı sekmelerini kapatın",
                ["ram_ok"] = "✅ RAM kullanımınız %{pct} — sağlıklı.\n\nHemen bir işlem yapmanıza gerek yok.",
                ["ram_generic"] = "💡 RAM İpuçları:\n1. Dashboard'da 'RAM Optimize Et' tıklayın\n2. Kullanılmayan uygulamaları kapatın\n3. Otomatik Modu etkinleştirin",
                ["cpu_high"] = "⚠️ CPU kullanımı %{pct} — oldukça yüksek!\n\nÖneriler:\n1. İşlemler sayfasında CPU kullanan uygulamaları kontrol edin\n2. Oyun Modunu etkinleştirin\n3. Otomatik Modu açın",
                ["cpu_ok"] = "✅ CPU kullanımı %{pct} — normal aralıkta.",
                ["cpu_generic"] = "💡 CPU İpuçları:\n1. Oyun Modunu etkinleştirin\n2. Otomatik Mod arka plan işlemlerini yönetir\n3. İşlemler sayfasını kontrol edin",
                ["ping_high"] = "⚠️ Ping {ms}ms — gecikme yaşayabilirsiniz!\n\nÖneriler:\n1. DNS önbelleğini temizleyin\n2. DNS Benchmark çalıştırın\n3. Wi-Fi yerine ethernet kullanın\n4. TCP optimizasyonlarını uygulayın",
                ["ping_ok"] = "✅ Ping {ms}ms — mükemmel!\n\nBağlantınız oyun ve yayın için uygun.",
                ["ping_generic"] = "💡 Ping Optimizasyonu:\n1. DNS önbelleğini temizleyin\n2. DNS Benchmark çalıştırın\n3. TCP optimizasyonlarını uygulayın\n4. 5GHz Wi-Fi veya ethernet tercih edin",
                ["lag_help"] = "🔍 Gecikme Sorun Giderme:\n1. Gecikme Analizi sayfasına gidin → 'Analiz Çalıştır'\n2. Dashboard'da ping ve paket kaybını kontrol edin\n3. Oyun Modunu etkinleştirin\n4. DNS temizleyin ve TCP optimizasyonlarını uygulayın\n5. Otomatik Modu açın",
                ["gaming_tips"] = "🎮 Oyun Performans İpuçları:\n1. Oyun Modunu etkinleştirin\n2. Otomatik Modu açın — otomatik olarak:\n   • Yüksek Performans güç planına geçer\n   • Nagle algoritmasını kapatır\n   • Görsel efektleri azaltır\n3. TCP optimizasyonlarını uygulayın\n4. Wi-Fi yerine ethernet kullanın",
                ["dns_help"] = "🌐 DNS Optimizasyonu:\n1. Ağ sayfası → 'DNS Benchmark'\n2. En hızlı DNS sunucusunu uygulayın\n3. DNS önbelleğini temizleyin\n\nDaha hızlı DNS = daha hızlı bağlantı.",
                ["speed_help"] = "⚡ Hız Testi:\n1. Hız Testi sayfasına gidin → 'Hız Testi Çalıştır'\n2. DNS Benchmark ile daha iyi yönlendirme bulun",
                ["cache_help"] = "🗑️ Önbellek Yönetimi:\n1. Önbellek sayfası → 'Tara'\n2. Tarayıcı önbelleklerini temizleyin\n3. Disk alanı boşaltır ve sorunları giderir",
                ["optimize_help"] = "⚡ Optimizasyon Rehberi:\n1. Hızlı: Dashboard'da 'Optimizasyonu Başlat'\n2. Ağ: DNS temizle + TCP optimize et\n3. Oyun: Oyun Modu + Otomatik Mod\n4. Gelişmiş: Nagle algoritmasını kapat\n\nOtomatik Mod her şeyi otomatik yapar!",
                ["auto_mode_help"] = "🤖 Otomatik Mod:\nEtkinleştirildiğinde NetProve otomatik olarak:\n• RAM %85'i aşınca optimize eder\n• Oyun sırasında arka plan işlemlerini kısar\n• Her 30 dakikada DNS temizler\n• Oyun için güç planını değiştirir\n• Düşük gecikme için Nagle'ı kapatır\n\nDashboard'dan açın. Güvenli ve sessiz çalışır!",
                ["streaming_tips"] = "📡 Yayın İpuçları:\n1. Yayın Modunu etkinleştirin\n2. TCP optimizasyonlarını uygulayın\n3. Gereksiz uygulamaları kapatın\n4. Ethernet kullanın",
                ["network_reset_help"] = "🔄 Ağ Sıfırlama:\nAğ sayfası → 'Ağ Yığınını Sıfırla'\n⚠️ Sıfırlama sonrası sistem yeniden başlatma önerilir.",
                ["nagle_help"] = "📡 Nagle Algoritması:\nKapatıldığında paketler hemen gönderilir.\nOyunlarda 5-15ms gecikme azaltır.\nOtomatik Mod bunu oyun sırasında otomatik yapar.",
                ["power_plan_help"] = "⚡ Güç Planı:\nYüksek Performans modu CPU kısıtlamasını önler.\nOtomatik Mod oyun sırasında otomatik geçiş yapar.",
                ["packet_loss_help"] = "📦 Paket Kaybı Çözümleri:\n1. Kablo bağlantılarını kontrol edin\n2. Modemi yeniden başlatın\n3. Ethernet kullanın\n4. DNS temizleyin\n5. %5'in üzerindeyse ISP'nizi arayın",
                ["jitter_help"] = "📊 Jitter Çözümleri:\n1. Ethernet bağlantı kullanın\n2. Bant genişliği kullanan uygulamaları kapatın\n3. DNS temizleyin\n4. TCP optimizasyonlarını uygulayın",
                ["wifi_help"] = "📶 Wi-Fi İpuçları:\n1. Ağ sayfasında bandınızı kontrol edin\n2. 5GHz daha hızlı ama menzili kısa\n3. Oyun için her zaman ethernet tercih edin",
                ["thanks"] = "Ne demek, her zaman buradayım! 😊 Başka bir sorunun olursa çekinme, iyi oyunlar!",
                ["nomatch"] = "Hmm, bu konuda tam emin değilim. 🤔 Ama şu konularda çok iyiyim:\n\n• RAM veya CPU performansı\n• Ping, gecikme veya ağ sorunları\n• Oyun ve yayın optimizasyonu\n• DNS hızı ve önbellek yönetimi\n• Otomatik mod ayarları\n\nSorununu farklı şekilde sormayı dene, elimden geleni yapacağım!",
                ["empty"] = "Seni dinliyorum! 💬 Aşağıya sorununu yaz, hemen yardımcı olayım. Sistem optimizasyonu, ağ sorunları, oyun ipuçları ve daha fazlası konusunda yardımcı olabilirim.",
            },
            [AppLanguage.Chinese] = new()
            {
                ["welcome"] = "你好！👋 我是你的 NetProve 助手。我会帮你从系统中获得最佳性能。\n\n无论是 RAM、网络问题、游戏优化还是减少延迟，都可以问我。我会实时检查你的系统并提供个性化建议。\n\n在下方输入你的问题吧！",
                ["greeting"] = "嗨！👋 很高兴见到你！我可以帮助你解决任何性能问题。延迟、网速慢、CPU 过高或游戏提示——随时问我！",
                ["capabilities"] = "🔧 我可以帮助：\n• RAM 和 CPU 优化\n• 延迟和网络问题\n• 游戏性能\n• 缓存清理\n• DNS 优化\n• 自动模式\n描述你的问题！",
                ["ram_high"] = "⚠️ RAM 使用率 {pct}% — 偏高！\n\n建议：\n1. 仪表板 → 点击「优化 RAM」\n2. 在进程页面检查占用内存的应用\n3. 开启自动模式",
                ["ram_ok"] = "✅ RAM 使用率 {pct}% — 正常。",
                ["ram_generic"] = "💡 RAM 提示：\n1. 点击仪表板上的「优化 RAM」\n2. 关闭不用的应用\n3. 开启自动模式",
                ["cpu_high"] = "⚠️ CPU 使用率 {pct}% — 很高！\n\n建议：\n1. 检查进程页面\n2. 开启游戏模式\n3. 开启自动模式",
                ["cpu_ok"] = "✅ CPU 使用率 {pct}% — 正常。",
                ["cpu_generic"] = "💡 CPU 提示：\n1. 开启游戏模式\n2. 自动模式管理后台进程",
                ["ping_high"] = "⚠️ 延迟 {ms}ms — 可能会卡！\n\n建议：\n1. 刷新 DNS\n2. 运行 DNS 基准测试\n3. 使用有线连接\n4. 应用 TCP 优化",
                ["ping_ok"] = "✅ 延迟 {ms}ms — 很好！",
                ["ping_generic"] = "💡 延迟优化：\n1. 刷新 DNS\n2. DNS 基准测试\n3. TCP 优化\n4. 使用 5GHz WiFi 或有线",
                ["lag_help"] = "🔍 延迟排查：\n1. 延迟分析页 → 运行分析\n2. 检查 ping 和丢包\n3. 开启游戏模式\n4. 刷新 DNS + TCP 优化\n5. 开启自动模式",
                ["gaming_tips"] = "🎮 游戏性能提示：\n1. 开启游戏模式\n2. 开启自动模式\n3. TCP 优化\n4. 使用有线连接",
                ["dns_help"] = "🌐 DNS 优化：\n1. 网络页 → DNS 基准测试\n2. 应用最快的 DNS\n3. 刷新 DNS 缓存",
                ["speed_help"] = "⚡ 速度测试：\n1. 速度测试页 → 运行测试\n2. DNS 基准测试优化路由",
                ["cache_help"] = "🗑️ 缓存管理：\n1. 缓存页 → 扫描\n2. 清理浏览器缓存\n3. 释放磁盘空间",
                ["optimize_help"] = "⚡ 优化指南：\n1. 快速：仪表板「开始优化」\n2. 网络：DNS + TCP\n3. 游戏：游戏模式 + 自动模式\n4. 高级：禁用 Nagle 算法",
                ["auto_mode_help"] = "🤖 自动模式：\n自动优化 RAM、DNS、进程优先级。\n游戏时切换高性能电源计划。\n从仪表板开启！",
                ["streaming_tips"] = "📡 直播提示：\n1. 开启直播模式\n2. TCP 优化\n3. 使用有线连接",
                ["network_reset_help"] = "🔄 网络重置：\n网络页 → 重置网络\n⚠️ 重置后建议重启系统。",
                ["nagle_help"] = "📡 Nagle 算法：\n禁用后降低 5-15ms 延迟。\n自动模式会自动处理。",
                ["power_plan_help"] = "⚡ 电源计划：\n高性能模式防止 CPU 降频。\n自动模式在游戏时自动切换。",
                ["packet_loss_help"] = "📦 丢包解决方案：\n1. 检查网线\n2. 重启路由器\n3. 使用有线连接\n4. 刷新 DNS",
                ["jitter_help"] = "📊 抖动解决：\n1. 使用有线连接\n2. 关闭占带宽的应用\n3. TCP 优化",
                ["wifi_help"] = "📶 WiFi 提示：\n1. 5GHz 更快\n2. 游戏用有线最好",
                ["thanks"] = "😊 不客气！有问题随时问。",
                ["nomatch"] = "🤔 不太确定。你可以问：\n• RAM/CPU 使用\n• 网络问题\n• 游戏优化\n• DNS/速度\n• 自动模式",
                ["empty"] = "💬 请输入问题。我可以帮助优化、网络、游戏等！",
            },
            [AppLanguage.Japanese] = new()
            {
                ["welcome"] = "こんにちは！👋 NetProve アシスタントです。システムから最高のパフォーマンスを引き出すお手伝いをします。\n\nRAM、ネットワーク問題、ゲーム最適化、ラグ対策など、何でも聞いてください。リアルタイムでシステムをチェックし、最適なアドバイスをお届けします。\n\n下に質問を入力してください！",
                ["greeting"] = "やあ！👋 お会いできて嬉しいです！パフォーマンスに関することなら何でもお手伝いします。ラグ、遅いネット、高いCPU、ゲームのコツ——何でも聞いてください！",
                ["capabilities"] = "🔧 サポート内容：\n• RAM/CPU最適化\n• Ping/ネットワーク\n• ゲーム性能\n• キャッシュ削除\n• DNS最適化\n• 自動モード\n質問をどうぞ！",
                ["ram_high"] = "⚠️ RAM使用率 {pct}% — 高いです！\n\n1. ダッシュボード → 「RAM最適化」\n2. プロセスページで確認\n3. 自動モードを有効に",
                ["ram_ok"] = "✅ RAM使用率 {pct}% — 正常です。",
                ["ram_generic"] = "💡 RAMのヒント：\n1.「RAM最適化」をクリック\n2. 不要なアプリを閉じる\n3. 自動モードを有効に",
                ["cpu_high"] = "⚠️ CPU使用率 {pct}% — 高いです！\n\n1. プロセスページで確認\n2. ゲームモードを有効に",
                ["cpu_ok"] = "✅ CPU使用率 {pct}% — 正常です。",
                ["cpu_generic"] = "💡 CPUのヒント：\n1. ゲームモードを有効に\n2. 自動モードで管理",
                ["ping_high"] = "⚠️ Ping {ms}ms — ラグの原因に！\n\n1. DNSフラッシュ\n2. DNSベンチマーク\n3. 有線接続を使用\n4. TCP最適化",
                ["ping_ok"] = "✅ Ping {ms}ms — 良好です！",
                ["ping_generic"] = "💡 Ping最適化：\n1. DNSフラッシュ\n2. DNSベンチマーク\n3. TCP最適化",
                ["lag_help"] = "🔍 ラグ対策：\n1. ラグ分析ページ → 分析実行\n2. ゲームモード有効\n3. DNS + TCP最適化\n4. 自動モード",
                ["gaming_tips"] = "🎮 ゲームパフォーマンス：\n1. ゲームモード有効\n2. 自動モード有効\n3. TCP最適化\n4. 有線接続推奨",
                ["dns_help"] = "🌐 DNS最適化：\n1. ネットワーク → DNSベンチマーク\n2. 最速DNSを適用\n3. DNSキャッシュクリア",
                ["speed_help"] = "⚡ 速度テスト：\n速度テストページで実行",
                ["cache_help"] = "🗑️ キャッシュ管理：\nキャッシュページ → スキャン → 削除",
                ["optimize_help"] = "⚡ 最適化ガイド：\n1. ダッシュボード「最適化開始」\n2. ゲームモード + 自動モード\n3. DNS + TCP最適化",
                ["auto_mode_help"] = "🤖 自動モード：\n自動でRAM、DNS、プロセスを最適化。\nゲーム時に電源プランを切替。\nダッシュボードから有効に！",
                ["streaming_tips"] = "📡 配信のヒント：\n1. 配信モード有効\n2. TCP最適化\n3. 有線接続",
                ["network_reset_help"] = "🔄 ネットワークリセット：\n⚠️ リセット後は再起動推奨。",
                ["nagle_help"] = "📡 Nagleアルゴリズム：\n無効で5-15ms低減。自動モードで自動処理。",
                ["power_plan_help"] = "⚡ 電源プラン：\n高パフォーマンスでCPU制限防止。自動モードで自動切替。",
                ["packet_loss_help"] = "📦 パケットロス対策：\n1. ケーブル確認\n2. ルーター再起動\n3. 有線接続\n4. DNSフラッシュ",
                ["jitter_help"] = "📊 ジッター対策：\n1. 有線接続\n2. 帯域使用アプリを閉じる\n3. TCP最適化",
                ["wifi_help"] = "📶 Wi-Fiのヒント：\n1. 5GHz推奨\n2. ゲームは有線が最適",
                ["thanks"] = "😊 どういたしまして！他にも質問があればどうぞ。",
                ["nomatch"] = "🤔 よくわかりません。以下を聞いてみてください：\n• RAM/CPU\n• ネットワーク\n• ゲーム最適化\n• DNS/速度",
                ["empty"] = "💬 質問を入力してください。最適化やネットワークについてサポートします！",
            },
            [AppLanguage.Spanish] = new()
            {
                ["welcome"] = "¡Hola! 👋 Soy tu asistente NetProve. Estoy aquí para ayudarte a obtener el mejor rendimiento de tu sistema.\n\nPuedes preguntarme sobre uso de RAM, problemas de red, optimización de juegos, o cómo reducir el lag. Revisaré tu sistema en tiempo real y te daré consejos personalizados.\n\n¡Escribe tu pregunta abajo!",
                ["greeting"] = "¡Hey! 👋 ¡Me alegra verte! Estoy aquí para ayudarte con todo lo relacionado al rendimiento. Lag, internet lento, CPU alto o tips de juegos — ¡solo pregunta!",
                ["capabilities"] = "🔧 Puedo ayudar con:\n• Optimización de RAM y CPU\n• Problemas de ping y red\n• Rendimiento en juegos\n• Limpieza de caché\n• Optimización DNS\n• Modo automático\n¡Describe tu problema!",
                ["ram_high"] = "⚠️ Uso de RAM: {pct}% — ¡alto!\n\n1. Panel → 'Optimizar RAM'\n2. Revisa procesos\n3. Activa Modo Auto",
                ["ram_ok"] = "✅ Uso de RAM: {pct}% — saludable.",
                ["ram_generic"] = "💡 Consejos RAM:\n1. Clic en 'Optimizar RAM'\n2. Cierra apps innecesarias\n3. Activa Modo Auto",
                ["cpu_high"] = "⚠️ Uso de CPU: {pct}% — ¡alto!\n\n1. Revisa procesos\n2. Activa Modo Juego\n3. Activa Modo Auto",
                ["cpu_ok"] = "✅ Uso de CPU: {pct}% — normal.",
                ["cpu_generic"] = "💡 Consejos CPU:\n1. Activa Modo Juego\n2. Modo Auto gestiona procesos",
                ["ping_high"] = "⚠️ Ping: {ms}ms — ¡puede causar lag!\n\n1. Limpia DNS\n2. Benchmark DNS\n3. Usa cable ethernet\n4. Optimiza TCP",
                ["ping_ok"] = "✅ Ping: {ms}ms — ¡excelente!",
                ["ping_generic"] = "💡 Optimización de ping:\n1. Limpia DNS\n2. Benchmark DNS\n3. Optimiza TCP",
                ["lag_help"] = "🔍 Solución de lag:\n1. Análisis de Lag → Ejecutar\n2. Revisa ping y pérdida\n3. Modo Juego\n4. DNS + TCP\n5. Modo Auto",
                ["gaming_tips"] = "🎮 Rendimiento en juegos:\n1. Activa Modo Juego\n2. Activa Modo Auto\n3. Optimiza TCP\n4. Usa ethernet",
                ["dns_help"] = "🌐 Optimización DNS:\n1. Red → Benchmark DNS\n2. Aplica el DNS más rápido\n3. Limpia caché DNS",
                ["speed_help"] = "⚡ Test de velocidad:\nPágina de velocidad → Ejecutar test",
                ["cache_help"] = "🗑️ Gestión de caché:\nPágina de caché → Escanear → Limpiar",
                ["optimize_help"] = "⚡ Guía de optimización:\n1. Rápido: Panel 'Iniciar Optimización'\n2. Red: DNS + TCP\n3. Juego: Modo Juego + Auto\n4. Avanzado: Desactivar Nagle",
                ["auto_mode_help"] = "🤖 Modo Auto:\nOptimiza RAM, DNS y procesos automáticamente.\nCambia plan de energía para juegos.\n¡Actívalo desde el panel!",
                ["streaming_tips"] = "📡 Consejos de streaming:\n1. Activa Modo Streaming\n2. Optimiza TCP\n3. Usa ethernet",
                ["network_reset_help"] = "🔄 Reseteo de red:\n⚠️ Reinicio recomendado después.",
                ["nagle_help"] = "📡 Algoritmo Nagle:\nDesactivar reduce 5-15ms. Modo Auto lo gestiona.",
                ["power_plan_help"] = "⚡ Plan de energía:\nAlto rendimiento evita throttling. Modo Auto cambia automáticamente.",
                ["packet_loss_help"] = "📦 Pérdida de paquetes:\n1. Revisa cables\n2. Reinicia router\n3. Usa ethernet\n4. Limpia DNS",
                ["jitter_help"] = "📊 Solución de jitter:\n1. Usa ethernet\n2. Cierra apps pesadas\n3. Optimiza TCP",
                ["wifi_help"] = "📶 Consejos Wi-Fi:\n1. 5GHz es más rápido\n2. Para juegos: ethernet siempre",
                ["thanks"] = "😊 ¡De nada! Si necesitas algo más, pregunta.",
                ["nomatch"] = "🤔 No estoy seguro. Prueba preguntar sobre:\n• RAM/CPU\n• Red/ping\n• Optimización\n• DNS/velocidad\n• Modo auto",
                ["empty"] = "💬 Escribe una pregunta. ¡Puedo ayudar con optimización, red, juegos y más!",
            },
            [AppLanguage.Russian] = new()
            {
                ["welcome"] = "Привет! 👋 Я твой ассистент NetProve. Помогу получить максимальную производительность от системы.\n\nСпрашивай о чём угодно — RAM, сетевые проблемы, оптимизация игр, снижение лагов. Я проверю систему в реальном времени и дам персональные советы.\n\nПиши свой вопрос ниже!",
                ["greeting"] = "Привет! 👋 Рад тебя видеть! Я помогу с любыми вопросами по производительности. Лаги, медленный интернет, высокая нагрузка CPU или игровые советы — просто спроси!",
                ["capabilities"] = "🔧 Могу помочь с:\n• Оптимизация RAM и CPU\n• Проблемы пинга и сети\n• Производительность в играх\n• Очистка кэша\n• Оптимизация DNS\n• Автоматический режим\nОпишите проблему!",
                ["ram_high"] = "⚠️ Использование RAM: {pct}% — высокое!\n\n1. Панель → «Оптимизировать RAM»\n2. Проверьте процессы\n3. Включите Авто-режим",
                ["ram_ok"] = "✅ Использование RAM: {pct}% — в норме.",
                ["ram_generic"] = "💡 Советы по RAM:\n1. Нажмите «Оптимизировать RAM»\n2. Закройте ненужные приложения\n3. Включите Авто-режим",
                ["cpu_high"] = "⚠️ Использование CPU: {pct}% — высокое!\n\n1. Проверьте процессы\n2. Включите Игровой режим\n3. Включите Авто-режим",
                ["cpu_ok"] = "✅ Использование CPU: {pct}% — в норме.",
                ["cpu_generic"] = "💡 Советы по CPU:\n1. Включите Игровой режим\n2. Авто-режим управляет процессами",
                ["ping_high"] = "⚠️ Пинг: {ms}мс — может быть лаг!\n\n1. Очистите DNS\n2. DNS бенчмарк\n3. Используйте ethernet\n4. TCP оптимизация",
                ["ping_ok"] = "✅ Пинг: {ms}мс — отлично!",
                ["ping_generic"] = "💡 Оптимизация пинга:\n1. Очистите DNS\n2. DNS бенчмарк\n3. TCP оптимизация",
                ["lag_help"] = "🔍 Устранение лага:\n1. Анализ лага → Запустить\n2. Проверьте пинг\n3. Игровой режим\n4. DNS + TCP\n5. Авто-режим",
                ["gaming_tips"] = "🎮 Советы для игр:\n1. Включите Игровой режим\n2. Включите Авто-режим\n3. TCP оптимизация\n4. Используйте ethernet",
                ["dns_help"] = "🌐 DNS оптимизация:\n1. Сеть → DNS бенчмарк\n2. Примените быстрый DNS\n3. Очистите DNS кэш",
                ["speed_help"] = "⚡ Тест скорости:\nСтраница теста → Запустить",
                ["cache_help"] = "🗑️ Управление кэшем:\nСтраница кэша → Сканировать → Очистить",
                ["optimize_help"] = "⚡ Руководство:\n1. Быстро: Панель «Начать оптимизацию»\n2. Сеть: DNS + TCP\n3. Игры: Игровой + Авто режим\n4. Продвинуто: Отключить Nagle",
                ["auto_mode_help"] = "🤖 Авто-режим:\nАвтоматически оптимизирует RAM, DNS, процессы.\nПереключает план питания для игр.\nВключите на панели!",
                ["streaming_tips"] = "📡 Советы для стрима:\n1. Включите Режим стрима\n2. TCP оптимизация\n3. Ethernet",
                ["network_reset_help"] = "🔄 Сброс сети:\n⚠️ Рекомендуется перезагрузка после сброса.",
                ["nagle_help"] = "📡 Алгоритм Nagle:\nОтключение уменьшает задержку на 5-15мс. Авто-режим управляет автоматически.",
                ["power_plan_help"] = "⚡ План питания:\nВысокая производительность предотвращает троттлинг. Авто-режим переключает автоматически.",
                ["packet_loss_help"] = "📦 Потеря пакетов:\n1. Проверьте кабели\n2. Перезагрузите роутер\n3. Используйте ethernet\n4. Очистите DNS",
                ["jitter_help"] = "📊 Устранение джиттера:\n1. Ethernet\n2. Закройте тяжёлые приложения\n3. TCP оптимизация",
                ["wifi_help"] = "📶 Советы по Wi-Fi:\n1. 5ГГц быстрее\n2. Для игр: всегда ethernet",
                ["thanks"] = "😊 Пожалуйста! Если есть ещё вопросы — спрашивайте.",
                ["nomatch"] = "🤔 Не уверен. Попробуйте спросить о:\n• RAM/CPU\n• Сеть/пинг\n• Оптимизация\n• DNS/скорость\n• Авто-режим",
                ["empty"] = "💬 Введите вопрос. Могу помочь с оптимизацией, сетью, играми!",
            },
        };

        private sealed class ChatRule
        {
            public string[] Keywords { get; }
            public string Category { get; }
            public int Priority { get; }
            public Func<string> ResponseGenerator { get; }

            public ChatRule(string[] keywords, string category, int priority, Func<string> responseGenerator)
            {
                Keywords = keywords;
                Category = category;
                Priority = priority;
                ResponseGenerator = responseGenerator;
            }
        }
    }
}
