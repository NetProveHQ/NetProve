package com.netprove.app.monitor

import android.app.ActivityManager
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.BatteryManager
import com.netprove.app.core.EventBus
import com.netprove.app.model.SystemMetrics
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.*
import java.io.RandomAccessFile
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class SystemMonitor @Inject constructor(
    @ApplicationContext private val context: Context,
    private val eventBus: EventBus
) {
    private var monitorJob: Job? = null
    private var previousCpuIdle = 0L
    private var previousCpuTotal = 0L

    fun start(scope: CoroutineScope, intervalMs: Long = 3000L) {
        monitorJob?.cancel()
        monitorJob = scope.launch(Dispatchers.IO) {
            while (isActive) {
                try {
                    val metrics = measure()
                    eventBus.publishSystemMetrics(metrics)
                } catch (_: CancellationException) {
                    break
                } catch (_: Exception) {
                    eventBus.publishSystemMetrics(SystemMetrics())
                }
                delay(intervalMs)
            }
        }
    }

    fun stop() {
        monitorJob?.cancel()
        monitorJob = null
    }

    private fun measure(): SystemMetrics {
        val cpu = readCpuUsage()
        val (ramUsed, ramTotal) = readRamUsage()
        val ramPercent = if (ramTotal > 0) (ramUsed.toFloat() / ramTotal) * 100f else 0f
        val (batteryPct, batteryTemp, charging) = readBattery()

        return SystemMetrics(
            cpuUsagePercent = cpu,
            ramUsagePercent = ramPercent,
            ramUsedMb = ramUsed / (1024 * 1024),
            ramTotalMb = ramTotal / (1024 * 1024),
            batteryPercent = batteryPct,
            batteryTemperature = batteryTemp,
            isCharging = charging
        )
    }

    private fun readCpuUsage(): Float {
        return try {
            val reader = RandomAccessFile("/proc/stat", "r")
            val line = reader.readLine()
            reader.close()

            val parts = line.split("\\s+".toRegex())
            // user, nice, system, idle, iowait, irq, softirq
            val idle = parts[4].toLong()
            val total = parts.drop(1).take(7).sumOf { it.toLong() }

            val diffIdle = idle - previousCpuIdle
            val diffTotal = total - previousCpuTotal
            previousCpuIdle = idle
            previousCpuTotal = total

            if (diffTotal > 0) {
                ((diffTotal - diffIdle).toFloat() / diffTotal) * 100f
            } else 0f
        } catch (_: Exception) {
            0f
        }
    }

    private fun readRamUsage(): Pair<Long, Long> {
        val am = context.getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
        val memInfo = ActivityManager.MemoryInfo()
        am.getMemoryInfo(memInfo)
        val total = memInfo.totalMem
        val available = memInfo.availMem
        return Pair(total - available, total)
    }

    private fun readBattery(): Triple<Int, Float, Boolean> {
        val batteryIntent = context.registerReceiver(null, IntentFilter(Intent.ACTION_BATTERY_CHANGED))
        val level = batteryIntent?.getIntExtra(BatteryManager.EXTRA_LEVEL, -1) ?: -1
        val scale = batteryIntent?.getIntExtra(BatteryManager.EXTRA_SCALE, -1) ?: -1
        val temp = batteryIntent?.getIntExtra(BatteryManager.EXTRA_TEMPERATURE, 0) ?: 0
        val status = batteryIntent?.getIntExtra(BatteryManager.EXTRA_STATUS, -1) ?: -1

        val pct = if (level >= 0 && scale > 0) (level * 100) / scale else 0
        val tempC = temp / 10f
        val isCharging = status == BatteryManager.BATTERY_STATUS_CHARGING ||
                status == BatteryManager.BATTERY_STATUS_FULL

        return Triple(pct, tempC, isCharging)
    }
}
