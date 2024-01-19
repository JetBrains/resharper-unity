package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.intellij.execution.util.ExecUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.diagnostic.trace
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.launchBackground
import com.jetbrains.rd.platform.diagnostics.doActivity
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.solution
import java.io.File
import java.nio.file.Path
import kotlin.concurrent.timer
import kotlin.io.path.isDirectory

class AndroidDeviceListener() {

    companion object {
        private val logger = Logger.getInstance(AndroidDeviceListener::class.java)
    }

    private data class DeviceDetails(private val properties: Map<String, String>) {
        val displayName: String
            get() {
                val manufacturer = properties["ro.product.manufacturer"] ?: properties["ro.product.brand"] ?: ""
                val model = properties["ro.product.model"] ?: properties["ro.product.name"] ?: ""
                return buildString {
                    append(manufacturer.replaceFirstChar { it.uppercase() })
                    if (isNotEmpty() && model.isNotEmpty()) append(" ")
                    append(model.replaceFirstChar { it.uppercase() })
                    if (isEmpty()) {
                        append(properties["ro.serialno"])
                    }
                }
            }
    }

    private data class PlayerDetails(val deviceSerial: String, val port: Int, val uid: String) {
        val key = "$deviceSerial-$port"
    }

    private val refreshPeriod = 1000L
    // Keyed by device serial ID
    private val deviceDetails = mutableMapOf<String, DeviceDetails>()
    // Both of these are keyed by player ID, a pair of device serial ID and port
    private val playerPackageNames = mutableMapOf<String, String?>()
    private val players = mutableMapOf<String, UnityAndroidAdbProcess>()

    private suspend fun getAdbPath(project: Project, lifetime: Lifetime): File? {
        try {
            val model = project.solution.frontendBackendModel
            val sdkRoot = model.getAndroidSdkRoot.startSuspending(lifetime, Unit)?.let { Path.of(it) }
                ?.takeIf { it.isDirectory() } ?: run {
                logger.trace("No Android SDK root from model. Trying to find manually")
                UnityInstallationFinder.getInstance(project).getAdditionalPlaybackEnginesRoot()
                    ?.resolve("AndroidPlayer")
                    ?.takeIf { it.isDirectory() }
            }

            if (sdkRoot == null) {
                logger.warn("Cannot find Android SDK tools. Skipping looking for Android devices")
                return null
            }

            logger.trace("Using Android SDK root: $sdkRoot")

            var adbExe = sdkRoot.resolve("SDK/platform-tools/adb").toFile()
            if (!adbExe.exists()) {
                adbExe = sdkRoot.resolve("SDK/platform-tools/adb.exe").toFile()
            }

            if (!adbExe.exists()) {
                logger.warn("Cannot find adb. Skipping looking for Android devices")
                return null
            }

            return adbExe
        } catch (e: Throwable) {
            logger.error(e)
            return null
        }
    }

    // Invoked on UI thread
    fun startListening(project: Project,
                       lifetime: Lifetime,
                       onPlayerAdded: (UnityProcess) -> Unit,
                       onPlayerRemoved: (UnityProcess) -> Unit) {
        // Since we're running in a modal dialog, we block the normal UI thread scheduler for getAdbPath. However, that
        // uses startSuspending, which will work with the appropriate scheduler. It's also really quick, as we're just
        // fetching a single string value, so we can use runBlocking and wait for the call to complete
        lifetime.launchBackground {
            val adbExe = getAdbPath(project, lifetime) ?: return@launchBackground

            val refreshTimer = timer("Poll for Android devices via adb", true, 0L, refreshPeriod) {
                refreshAndroidProcesses(adbExe, onPlayerAdded, onPlayerRemoved)
            }
            lifetime.onTermination { refreshTimer.cancel() }
        }
    }

    // Invoked on background thread
    suspend fun getPlayersForDevice(project: Project, deviceId: String): List<UnityProcess> {
        val lifetime = UnityProjectLifetimeService.getLifetime(project).createNested()
        try {
            val adbExe = getAdbPath(project, lifetime) ?: return emptyList()
            val players = mutableListOf<UnityProcess>()
            refreshAndroidProcesses(adbExe, listOf(deviceId), { players.add(it) }, { players.remove(it) })
            return players
        }
        finally {
            lifetime.terminate()
        }
    }

    private fun refreshAndroidProcesses(
        adbExe: File,
        onPlayerAdded: (UnityProcess) -> Unit,
        onPlayerRemoved: (UnityProcess) -> Unit
    ) {
        val discoveredDevices = getDevices(adbExe)
        updateCachedDeviceDetails(adbExe, discoveredDevices)
        refreshAndroidProcesses(adbExe, discoveredDevices, onPlayerAdded, onPlayerRemoved)
    }

    private fun refreshAndroidProcesses(
        adbExe: File,
        devices: List<String>,
        onPlayerAdded: (UnityProcess) -> Unit,
        onPlayerRemoved: (UnityProcess) -> Unit
    ) {
        val discoveredPlayers = getPlayers(adbExe, devices)
        updateCachedPackageNames(adbExe, discoveredPlayers)

        // Remove any players that are no longer available
        players.keys.filter { !discoveredPlayers.containsKey(it) }.forEach { key ->
            logger.trace("Removing old Android player $key")
            players.remove(key)?.let { onPlayerRemoved(it) }
        }

        // Add any players that we haven't seen before
        discoveredPlayers.values.filter { !players.containsKey(it.key) }.forEach { player ->
            val device = deviceDetails[player.deviceSerial]

            // Unity uses e.g. HP_Touchpad@ADB:{Serial} as a device ID
            val deviceName = device?.displayName
            val displayName = if (deviceName != null) "AndroidPlayer($deviceName)" else "AndroidPlayer"
            val packageName = playerPackageNames[player.key]

            logger.trace("Adding new Android player ${player.key} - ${deviceName ?: player.deviceSerial}")
            UnityAndroidAdbProcess(displayName, player.deviceSerial, deviceName, player.port, player.uid, packageName).let {
                players[player.key] = it
                onPlayerAdded(it)
            }
        }
    }

    private fun getDevices(adbExe: File): List<String> {
        val devices = mutableListOf<String>()
        invokeAdb("Calling adb to list Android devices", adbExe, "devices") { output ->
            @Suppress("SpellCheckingInspection")
            // Output will be one line for each device, in a similar format to this:
            // OOQTU1STFTV58I6DIRUSB4URTNQ6FGC2 device
            // or for 'adb devices -l': (I'm not certain of the order of these extra values, or what the
            // difference between `product:` and `device:`
            // OOQTU1STFTV58I6DIRUSB4URTNQ6FGC2 device usb:336855040X product:touchpad model:Touchpad device:tenderloin transport_id:2
            val simplePattern = Regex("""^(?<serial>[a-zA-Z0-9\-]+)(\s+)(device).*""")
            output.stdoutLines.forEach { line ->
                simplePattern.matchEntire(line)?.groups?.get("serial")?.value?.let {
                    devices.add(it)
                }
            }
        }
        return devices
    }

    private fun updateCachedDeviceDetails(adbExe: File, devices: List<String>) {
        devices.forEach { serial ->
            deviceDetails.computeIfAbsent(serial) { getDeviceDetails(adbExe, it) }
        }
    }

    private fun getDeviceDetails(adbExe: File, serial: String): DeviceDetails {
        val properties = mutableMapOf<String, String>()
        invokeAdb("Calling adb to retrieve device properties", adbExe, "-s", serial, "shell", "getprop") { output ->
            val regex = Regex("""^\[(?<key>.*)]: \[(?<value>.*)]$""")
            output.stdoutLines.forEach { line ->
                regex.matchEntire(line)?.let {
                    val key = it.groups["key"]?.value
                    val value = it.groups["value"]?.value

                    if (key == null || value == null) {
                        logger.trace("Unable to parse property: $line")
                    } else {
                        properties[key] = value
                    }
                }
            }
        }
        return DeviceDetails(properties)
    }

    private fun getPlayers(adbExe: File, devices: List<String>): Map<String, PlayerDetails> {
        val playerDetails = mutableMapOf<String, PlayerDetails>()
        devices.forEach { serial ->
            invokeAdb("Calling adb to retrieve Unity processes for $serial", adbExe, "-s", serial, "shell", "cat /proc/net/tcp") { output ->
                @Suppress("SpellCheckingInspection", "RegExpRepeatedSpace")
                // Output will be something like:
                //   sl  local_address rem_address   st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode
                //   0: 00000000:36D3 00000000:0000 0A 00000000:00000000 00:00000000 00000000  1041        0 3069 1 00000000 100 0 0 10 -1
                //   1: 0100007F:88B6 00000000:0000 0A 00000000:00000000 00:00000000 00000000  2000        0 5434 1 00000000 100 0 0 10 -1
                //   2: E801A8C0:B840 EEB3FA8E:01BB 08 00000000:00000082 00:00000000 00000000 10069        0 27883 1 00000000 23 4 30 10 -1
                // We want to get the port (in hex) from the local address, and the uid (in dec)
                // We also only look at localhost addresses - 0000000
                val pattern = Regex("""
                 |^\s*\d+:                                # sl
                 |\s+00000000:(?<port>[a-zA-Z0-9]+)       # local_address
                 |\s+[a-zA-Z0-9]+:[a-zA-Z0-9]+            # rem_address
                 |\s+[a-zA-Z0-9]+                         # st
                 |\s+[a-zA-Z0-9]+:[a-zA-Z0-9]+            # tx_queue : rx_queue
                 |\s+[a-zA-Z0-9]+:[a-zA-Z0-9]+            # tr : tm->when
                 |\s+[a-zA-Z0-9]+                         # retrnsmt
                 |\s+(?<uid>\d+)                          # uid
                 |.*""".trimMargin(), RegexOption.COMMENTS)

                output.stdoutLines.forEach { line ->
                    pattern.matchEntire(line)?.let { match ->
                        val port = match.groups["port"]?.value?.toInt(16)
                        val uid = match.groups["uid"]?.value
                        if (port != null && uid != null) {
                            if (port in 56000..57000) {
                                logger.trace("Found potential player on port $port with uid $uid")
                                PlayerDetails(serial, port, uid).let {
                                    playerDetails[it.key] = it
                                }
                            }
                        } else {
                            logger.warn("Unable to get capture values from string: $line")
                        }
                    }
                }
            }
        }
        return playerDetails
    }

    private fun updateCachedPackageNames(adbExe: File, players: Map<String, PlayerDetails>) {
        players.values.forEach { player ->
            playerPackageNames.computeIfAbsent(player.key) { getPackageName(adbExe, player) }
        }
    }

    private fun getPackageName(adbExe: File, player: PlayerDetails): String? {
        logger.doActivity("Calling adb to retrieve package name for uid ${player.uid}") {
            val output = execAndGetOutput(adbExe, "-s", player.deviceSerial, "shell", "cmd package list packages --uid ${player.uid}")
            if (output.exitCode == 0) {
                // Requires Android 8.x and above. Will output something like:
                // package:com.DefaultCompany.TowerDefense uid:10001\n
                Regex("""^package:(?<packageName>\S+) uid:.*\n?""").matchEntire(output.stdout)?.let {
                    return it.groups["packageName"]?.value
                }
            }
        }
        return null
    }

    private fun invokeAdb(key: String, adbExe: File, vararg args: String, action: (ProcessOutput) -> Unit) {
        logger.doActivity(key) {
            val output = execAndGetOutput(adbExe, *args)
            if (output.exitCode == 0) {
                action(output)
            }
        }
    }

    private fun execAndGetOutput(adbExe: File, vararg args: String): ProcessOutput {
        val commandLine = GeneralCommandLine(adbExe.toString(), *args)
        val output = ExecUtil.execAndGetOutput(commandLine)
        logger.trace {
            buildString {
                appendLine("Call to '${commandLine.commandLineString}'. Exit code ${output.exitCode}")
                appendLine("stdout:")
                appendLine(output.stdout)
                appendLine("stderr:")
                appendLine(output.stderr)
            }
        }
        if (output.exitCode != 0) {
            logger.warn("Call to '${commandLine.commandLineString}' failed. Exit code ${output.exitCode}")
            logger.warn("stderr:\n" + output.stderr)
        }
        return output
    }
}
