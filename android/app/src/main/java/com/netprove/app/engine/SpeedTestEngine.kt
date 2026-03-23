package com.netprove.app.engine

import com.netprove.app.model.SpeedTestPhase
import com.netprove.app.model.SpeedTestProgress
import com.netprove.app.model.SpeedTestResult
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flowOn
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.net.InetSocketAddress
import java.net.Socket
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class SpeedTestEngine @Inject constructor() {

    private val client = OkHttpClient.Builder()
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()

    private val downloadUrl = "https://speed.cloudflare.com/__down?bytes=10000000" // 10MB for mobile
    private val uploadUrl = "https://speed.cloudflare.com/__up"

    fun run(): Flow<SpeedTestProgress> = flow {
        emit(SpeedTestProgress(SpeedTestPhase.Ping, 0.0, 0.0))

        // Phase 1: Ping
        val pingMs = measurePing()
        emit(SpeedTestProgress(SpeedTestPhase.Ping, 100.0, pingMs))

        // Phase 2: Download
        emit(SpeedTestProgress(SpeedTestPhase.Download, 0.0, 0.0))
        val downloadMbps = measureDownload { progress, speed ->
            emit(SpeedTestProgress(SpeedTestPhase.Download, progress, speed))
        }

        // Phase 3: Upload
        emit(SpeedTestProgress(SpeedTestPhase.Upload, 0.0, 0.0))
        val uploadMbps = measureUpload { progress, speed ->
            emit(SpeedTestProgress(SpeedTestPhase.Upload, progress, speed))
        }

        // Complete
        emit(SpeedTestProgress(SpeedTestPhase.Complete, 100.0, 0.0))

        lastResult = SpeedTestResult(
            pingMs = pingMs,
            downloadMbps = downloadMbps,
            uploadMbps = uploadMbps
        )
    }.flowOn(Dispatchers.IO)

    var lastResult: SpeedTestResult? = null
        private set

    private fun measurePing(): Double {
        return try {
            val times = mutableListOf<Double>()
            repeat(3) {
                val socket = Socket()
                val start = System.nanoTime()
                socket.connect(InetSocketAddress("speed.cloudflare.com", 443), 3000)
                val elapsed = (System.nanoTime() - start) / 1_000_000.0
                socket.close()
                times.add(elapsed)
            }
            times.average()
        } catch (_: Exception) {
            -1.0
        }
    }

    private suspend fun measureDownload(
        onProgress: suspend (Double, Double) -> Unit
    ): Double {
        return try {
            val request = Request.Builder().url(downloadUrl).build()
            val response = client.newCall(request).execute()
            val body = response.body ?: return 0.0
            val totalBytes = body.contentLength().takeIf { it > 0 } ?: 10_000_000L

            val stream = body.byteStream()
            val buffer = ByteArray(8192)
            var downloaded = 0L
            val start = System.nanoTime()

            while (true) {
                val read = stream.read(buffer)
                if (read == -1) break
                downloaded += read

                val elapsed = (System.nanoTime() - start) / 1_000_000_000.0
                val currentMbps = if (elapsed > 0) (downloaded * 8.0) / elapsed / 1_000_000.0 else 0.0
                val progress = (downloaded.toDouble() / totalBytes * 100).coerceAtMost(100.0)
                onProgress(progress, currentMbps)
            }

            stream.close()
            response.close()

            val totalElapsed = (System.nanoTime() - start) / 1_000_000_000.0
            if (totalElapsed > 0) (downloaded * 8.0) / totalElapsed / 1_000_000.0 else 0.0
        } catch (_: Exception) {
            0.0
        }
    }

    private suspend fun measureUpload(
        onProgress: suspend (Double, Double) -> Unit
    ): Double {
        return try {
            val uploadSize = 2_000_000 // 2MB for mobile upload
            val data = ByteArray(uploadSize) { 0x41 }

            val start = System.nanoTime()
            val requestBody = data.toRequestBody("application/octet-stream".toMediaType())
            val request = Request.Builder()
                .url(uploadUrl)
                .post(requestBody)
                .build()

            onProgress(50.0, 0.0)
            client.newCall(request).execute().close()

            val totalElapsed = (System.nanoTime() - start) / 1_000_000_000.0
            val mbps = if (totalElapsed > 0) (uploadSize * 8.0) / totalElapsed / 1_000_000.0 else 0.0
            onProgress(100.0, mbps)
            mbps
        } catch (_: Exception) {
            0.0
        }
    }
}
