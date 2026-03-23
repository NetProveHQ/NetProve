package com.netprove.app.engine

import android.app.usage.UsageStatsManager
import android.content.Context
import android.content.pm.ApplicationInfo
import android.content.pm.PackageManager
import com.netprove.app.core.EventBus
import com.netprove.app.model.GameDetectedEvent
import com.netprove.app.model.GameEndedEvent
import com.netprove.app.model.GameSession
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.*
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class GameDetector @Inject constructor(
    @ApplicationContext private val context: Context,
    private val eventBus: EventBus
) {
    private var monitorJob: Job? = null
    var currentSession: GameSession? = null
        private set

    private var lastDetectedPackage: String? = null

    // Known game packages
    private val knownGames = mapOf(
        "com.riotgames.league.wildrift" to "Wild Rift",
        "com.riotgames.league.teamfighttactics" to "TFT",
        "com.activision.callofduty.shooter" to "Call of Duty Mobile",
        "com.tencent.ig" to "PUBG Mobile",
        "com.pubg.krmobile" to "PUBG Mobile KR",
        "com.pubg.newstate" to "PUBG: New State",
        "com.supercell.clashofclans" to "Clash of Clans",
        "com.supercell.clashroyale" to "Clash Royale",
        "com.supercell.brawlstars" to "Brawl Stars",
        "com.miHoYo.GenshinImpact" to "Genshin Impact",
        "com.HoYoverse.hkrpgoversea" to "Honkai: Star Rail",
        "com.garena.game.fctrl" to "Free Fire",
        "com.dts.freefiremax" to "Free Fire MAX",
        "com.mobile.legends" to "Mobile Legends",
        "com.mojang.minecraftpe" to "Minecraft",
        "com.innersloth.spacemafia" to "Among Us",
        "com.kiloo.subwaysurf" to "Subway Surfers",
        "com.epicgames.fortnite" to "Fortnite",
        "com.ea.gp.fifamobile" to "EA FC Mobile",
        "com.ea.game.pvzfree_row" to "Plants vs Zombies",
        "com.gameloft.android.ANMP.GloftA9HM" to "Asphalt 9",
        "com.tencent.tmgp.pubgmhd" to "PUBG Mobile HD",
        "com.netease.l10" to "Diablo Immortal",
        "com.blizzard.diablo.immortal" to "Diablo Immortal",
        "com.plarium.raidlegends" to "Raid: Shadow Legends",
        "com.kabam.marvelbattle" to "Marvel CoC",
        "com.king.candycrushsaga" to "Candy Crush",
        "com.igg.android.lordsmobile" to "Lords Mobile",
        "com.lilithgames.hgame.gp" to "AFK Arena",
        "jp.konami.pesam" to "eFootball",
        "com.dena.a12026418" to "Pokemon Masters",
        "com.nianticlabs.pokemongo" to "Pokemon GO",
        "com.vng.pubgmobile" to "PUBG Mobile VN",
        "com.tencent.iglite" to "PUBG Mobile Lite",

        // Battle Royale
        "com.ea.gp.apexlegendsmobile" to "Apex Legends Mobile",
        "com.netease.lztgglobal" to "Knives Out",
        "com.netease.mrzhna" to "Rules of Survival",
        "com.tencent.tmgp.sgame" to "Honor of Kings",
        "com.garena.game.codm" to "Call of Duty Mobile (Garena)",
        "com.activision.callofduty.warzone" to "Warzone Mobile",

        // Racing
        "com.ea.games.r3_row" to "Real Racing 3",
        "com.ea.game.nfs14_row" to "Need for Speed: No Limits",
        "com.gameloft.android.ANMP.GloftA8HM" to "Asphalt 8",
        "com.fingersoft.hillclimb" to "Hill Climb Racing",
        "com.fingersoft.hcr2" to "Hill Climb Racing 2",
        "com.naturalmotion.customstreetracer2" to "CSR Racing 2",
        "com.imaaginationgames.racemax" to "Race Max Pro",
        "com.vectorunit.silver.googleplay" to "Riptide GP: Renegade",
        "com.codemasters.F1Mobile" to "F1 Mobile Racing",
        "com.gameloft.android.GlsoftOverdrive" to "Asphalt Overdrive",
        "com.sygic.aura" to "Road Rush Cars",
        "com.topgames.bossrush" to "Rush Rally 3",

        // Strategy
        "com.lilithgame.roc.gp" to "Rise of Kingdoms",
        "com.supercell.clashquest" to "Clash Quest",
        "com.supercell.scroll" to "Clash Mini",
        "com.supercell.hayday" to "Hay Day",
        "com.supercell.boom" to "Boom Beach",
        "com.flaregames.royalrevolt2" to "Royal Revolt 2",
        "com.innogames.forgeofempires" to "Forge of Empires",
        "com.elex.nikkigp" to "Infinity Kingdom",
        "com.tap4fun.brutalage.gp" to "Age of Apes",
        "com.yotta.fra" to "Top War: Battle Game",
        "com.im30.ROE" to "Rise of Empires",
        "com.funplus.kingofavalon" to "King of Avalon",
        "com.IGG.castleclash" to "Castle Clash",
        "com.elex.worldwar" to "Last Shelter: Survival",

        // RPG / Gacha
        "com.YoStarEN.Arknights" to "Arknights",
        "com.aniplex.fategrandorder.en" to "Fate/Grand Order",
        "com.bandainamcoent.dblegends_ww" to "Dragon Ball Legends",
        "com.pearlabyss.blackdesertm.gl" to "Black Desert Mobile",
        "com.netmarble.nanagb" to "Ni no Kuni: Cross Worlds",
        "com.netmarble.linerangers" to "LINE Rangers",
        "com.netmarble.knights" to "Seven Knights",
        "com.netmarble.mherosgb" to "Marvel Future Fight",
        "com.nexon.maplem.global" to "MapleStory M",
        "com.com2us.smon.normal.freefull.google.kr.android.common" to "Summoners War",
        "com.ngames.ss.android" to "Saint Seiya Awakening",
        "com.sega.sonic4ep2thd" to "Sonic 4 Episode II",
        "com.squareenix.android_googleplay.FFBEWW" to "FF Brave Exvius",
        "com.square_enix.android_googleplay.war" to "War of the Visions",
        "com.miHoYo.bh3global" to "Honkai Impact 3rd",
        "com.levelinfinite.hotta.gp" to "Tower of Fantasy",
        "com.yostarjp.bluearchive" to "Blue Archive",
        "com.proximabeta.nikke" to "Goddess of Victory: Nikke",
        "com.gryphline.exastris" to "Reverse: 1999",
        "com.kurogame.wutheringwaves" to "Wuthering Waves",
        "com.HoYoverse.Nap" to "Zenless Zone Zero",

        // FPS
        "com.dts.freefireth" to "Free Fire TH",
        "com.riotgames.valorant.android" to "Valorant Mobile",
        "com.criticalforceentertainment.criticalops" to "Critical Ops",
        "com.axlebolt.standoff2" to "Standoff 2",
        "com.madfingergames.deadtrigger2" to "Dead Trigger 2",
        "com.firsttouchgames.smp" to "Sniper 3D",
        "com.gamedevltd.modernstrike" to "Modern Strike Online",
        "com.gameloft.android.ANMP.GloftM5HM" to "Modern Combat 5",
        "com.nfinityGames.fps.shooter" to "Special Forces Group 2",
        "com.Maskgun.Maskgun" to "MaskGun",
        "com.bluestacks.BSTNmarshal" to "Bullet Force",

        // Sports
        "com.ea.gp.fifaultimate" to "EA FC Mobile 24",
        "com.firsttouchgames.dls7" to "Dream League Soccer",
        "com.firsttouchgames.ssp3" to "Score! Hero",
        "com.miniclip.eightballpool" to "8 Ball Pool",
        "com.miniclip.golfbattle" to "Golf Battle",
        "com.nba2k.nba2kmobile" to "NBA 2K Mobile",
        "com.naturalmotion.clashofkings" to "Badminton Clash",
        "com.tencent.nba2kol2" to "NBA Infinite",
        "com.miniclip.basketballstars" to "Basketball Stars",
        "com.miniclip.miniclipfootball" to "Miniclip Football Strike",
        "com.realfootball.zkmf" to "Real Football",

        // MOBA
        "com.ngame.allstar.eu" to "Arena of Valor",
        "com.vainglory.vainglory" to "Vainglory",
        "com.mobilelegends.hwag" to "Mobile Legends: Adventure",
        "com.shatteredpixel.shatteredpixeldungeon" to "Omega Legends",
        "com.drodo.autochess" to "Auto Chess",
        "com.garena.game.moba" to "Garena Speed Drifters",
        "com.tencent.tmgp.cf" to "CrossFire: Legends",
        "com.proximabeta.mf.uamo" to "Marvel Super War",

        // Casual / Popular
        "com.roblox.client" to "Roblox",
        "com.miniclip.agar.io" to "Agar.io",
        "com.voodoo.snake_vs_block" to "Snake VS Block",
        "com.ketchapp.knife.hit" to "Knife Hit",
        "com.imangi.templerun2" to "Temple Run 2",
        "com.halfbrick.fruitninjafree" to "Fruit Ninja",
        "com.halfbrick.jetpackjoyride" to "Jetpack Joyride",
        "com.outfit7.talkingtom2" to "Talking Tom 2",
        "com.rovio.angrybirds" to "Angry Birds",
        "com.nekki.shadowfight3" to "Shadow Fight 3",
        "com.nekki.shadowfight" to "Shadow Fight 2",
        "com.playrix.township" to "Township",
        "com.playrix.homescapes" to "Homescapes",
        "com.playrix.gardenscapes" to "Gardenscapes",
        "com.king.farmheroessaga" to "Farm Heroes Saga",
        "com.king.bubblewitch3" to "Bubble Witch 3",
        "com.zynga.words3" to "Words With Friends 2",
        "com.zynga.livepoker" to "Zynga Poker",
        "com.ludo.king" to "Ludo King",
        "com.scopely.monopolygo" to "Monopoly GO!",
        "com.habby.archero" to "Archero",
        "com.habby.survivorio" to "Survivor.io",
        "com.RobTop.GeometryDashLite" to "Geometry Dash",

        // Turkish Popular Games
        "com.peakgames.toon" to "Toon Blast",
        "com.peakgames.matchington" to "Matchington Mansion",
        "com.gram.games.mergedragons" to "Merge Dragons",
        "com.gramgames.oneline" to "1010!",
        "com.peakgames.Okey" to "Okey",
        "com.peakgames.spades" to "Spades Royale",
        "com.masomo.headball2" to "Head Ball 2",
        "com.masomo.headball" to "Online Head Ball",
        "com.generagames.zulaman" to "Zula Mobile",
        "com.tfrconline.app" to "TFR Online",
        "com.pisti.card.game" to "Pisti",
        "com.zuuks.trafficrider" to "Traffic Rider",
        "com.zuuks.trafficracer" to "Traffic Racer",
        "com.codeworth.bahane" to "Tavla Plus",
        "com.digitalalchemy.batak.hd" to "Batak HD",
        "com.peak.saloon" to "Lost in Balkan"
    )

    fun start(scope: CoroutineScope, intervalMs: Long = 10000L) {
        monitorJob?.cancel()
        monitorJob = scope.launch(Dispatchers.IO) {
            while (isActive) {
                try {
                    checkForegroundApp()
                } catch (_: Exception) { }
                delay(intervalMs)
            }
        }
    }

    fun stop() {
        monitorJob?.cancel()
        monitorJob = null
    }

    private suspend fun checkForegroundApp() {
        val usageStatsManager = try {
            context.getSystemService(Context.USAGE_STATS_SERVICE) as? UsageStatsManager ?: return
        } catch (_: Exception) { return }

        val endTime = System.currentTimeMillis()
        val beginTime = endTime - 5000

        val stats = usageStatsManager.queryUsageStats(
            UsageStatsManager.INTERVAL_DAILY, beginTime, endTime
        )
        if (stats.isNullOrEmpty()) return

        val recentApp = stats.maxByOrNull { it.lastTimeUsed }?.packageName ?: return
        val isGame = isGamePackage(recentApp)

        if (isGame && recentApp != lastDetectedPackage) {
            val gameName = knownGames[recentApp] ?: getAppName(recentApp)
            lastDetectedPackage = recentApp
            currentSession = GameSession(packageName = recentApp, gameName = gameName)
            eventBus.publishGameDetected(GameDetectedEvent(recentApp, gameName))
        } else if (!isGame && lastDetectedPackage != null) {
            val session = currentSession
            if (session != null) {
                session.endTime = System.currentTimeMillis()
                eventBus.publishGameEnded(GameEndedEvent(session.packageName, session.gameName))
            }
            lastDetectedPackage = null
            currentSession = null
        }
    }

    private fun isGamePackage(packageName: String): Boolean {
        if (knownGames.containsKey(packageName)) return true
        return try {
            val appInfo = context.packageManager.getApplicationInfo(packageName, 0)
            appInfo.category == ApplicationInfo.CATEGORY_GAME
        } catch (_: PackageManager.NameNotFoundException) {
            false
        }
    }

    private fun getAppName(packageName: String): String {
        return try {
            val appInfo = context.packageManager.getApplicationInfo(packageName, 0)
            context.packageManager.getApplicationLabel(appInfo).toString()
        } catch (_: Exception) {
            packageName.substringAfterLast('.')
        }
    }
}
