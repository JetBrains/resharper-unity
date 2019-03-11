package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.OSProcessUtil
import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import java.net.*
import java.util.*
import java.util.regex.Pattern

class UnityProcessListener(private val onPlayerAdded: (UnityPlayer) -> Unit, private val onPlayerRemoved: (UnityPlayer) -> Unit) {

    companion object {
        private val logger = Logger.getInstance(UnityProcessListener::class.java)
    }

    // As far as I can tell:
    // * IP - where the process is running. On iPhone (and perhaps other devices) this can be the mobile data IP, which
    //        might be unreachable from this subnet, so be prepared to use the IP address from the UDP packet instead
    // * port - NOT the debugging port. I think this is the port uses to connect to the player (to e.g. get logs)
    // * flags - settings for the editor. Don't know what the values are
    // * guid - random number. Consistent only for the lifetime of the player. If no debugging port is found as part of
    //          `id`, then the debugging port is `guid % 1000 + 56000` (which belies the part that it's a random number,
    //          and is more likely that if no debugging port is specified, this must be a PID)
    // * editorId - random number representing an ID of the editor instance that built this player. Consistent for the
    //              lifetime of the editor. Will also be used by any other player built by the same editor instance.
    // * version - static value. Never been changed
    // * id - a textual identifier, e.g. "OSXPlayer(Matts.MacBookPro.Local)". May also include debugging port, e.g.
    //        `iPhonePlayer(Matts.iPhone7):56000`
    // * Debug - 0 or 1 to show if debugging is enabled. Will not be able to attach if this is 0
    // * [PackageName] .* - optional value. e.g. `iPhonePlayer`. I don't know how this is different to the value in id
    private val unityPlayerDescriptorRegex = Pattern.compile("""\[IP] (?<ip>.*) \[Port] (?<port>.*) \[Flags] (?<flags>.*) \[Guid] (?<guid>.*) \[EditorId] (?<editorid>.*) \[Version] (?<version>.*) \[Id] (?<id>[^:]+)(:(?<debuggerPort>\d+))? \[Debug] (?<debug>.*)""")

    private val defaultHeartbeat = 30

    private val refreshPeriod: Long = 100
    private val waitOnSocketLength = 50
    private val multicastPorts = listOf(54997, 34997, 57997, 58997)
    private val playerMulticastGroup = "225.0.0.222"
    private val multicastSockets = mutableListOf<MulticastSocket>()

    private val unityPlayerDescriptorsHeartbeats = mutableMapOf<String, Int>()
    private val unityPlayers = mutableMapOf<String, UnityPlayer>()

    private val refreshTimer: Timer
    private val socketsLock = Object()

    init {
        for (networkInterface in NetworkInterface.getNetworkInterfaces()) {
            if (!networkInterface.isUp || !networkInterface.supportsMulticast()
                    || !networkInterface.inetAddresses.asSequence().any { it is Inet4Address }) {
                continue
            }

            synchronized(socketsLock) {
                for (port in multicastPorts) {
                    try {
                        val multicastSocket = MulticastSocket(port)
                        val address = InetAddress.getByName(playerMulticastGroup)
                        multicastSocket.reuseAddress = true
                        multicastSocket.networkInterface = networkInterface
                        multicastSocket.soTimeout = waitOnSocketLength
                        multicastSocket.joinGroup(address)
                        multicastSockets.add(multicastSocket)
                    } catch (e: Exception) {
                        logger.warn(e.message)
                    }
                }
            }
        }

        refreshTimer = kotlin.concurrent.timer("Listen for Unity Players", true, 0L, refreshPeriod) {
            refreshUnityPlayersList()
        }

        OSProcessUtil.getProcessList().filter { UnityRunUtil.isUnityEditorProcess(it) }.map { processInfo ->
            val port = convertPidToDebuggerPort(processInfo.pid)
            UnityPlayer("127.0.0.1", port, port, 0, port.toLong(), port.toLong(), 0, processInfo.executableName, true, true)
        }.forEach {
            onPlayerAdded(it)
        }
    }

    private fun parseUnityPlayer(unityPlayerDescriptor: String, hostAddress: String): UnityPlayer? {
        try {
            val matcher = unityPlayerDescriptorRegex.matcher(unityPlayerDescriptor)
            if (matcher.find()) {
//                val ip = matcher.group("ip")
                val port = matcher.group("port").toInt()
                val flags = matcher.group("flags").toLong()
                val guid = matcher.group("guid").toLong()
                val editorGuid = matcher.group("editorid").toLong()
                val version = matcher.group("version").toInt()
                val id = matcher.group("id")
                val allowDebugging = matcher.group("debug").startsWith("1")
                var debuggerPort = 0
                try {
                    debuggerPort = matcher.group("debuggerPort").toInt()
                } catch (e: Exception) {
                    //ignore errors on debuggerPort matching or parsing
                }
                if (debuggerPort == 0) {
                    // It's not actually a pid, but it's what we have to do
                    debuggerPort = convertPidToDebuggerPort(guid)
                }

                // We use hostAddress instead of ip because this is the address we actually received the mulitcast from.
                // This is more accurate than what we're told, because the Unity process might be reporting the IP address
                // of an interface that isn't reachable. For example, the iPhone player can report the local IP address
                // of the mobile data network, which we can't reach from the current network (if we disable mobile data
                // it works as expected)
                return UnityPlayer(hostAddress, port, debuggerPort, flags, guid, editorGuid, version, id, allowDebugging, false)
            }
        } catch (e: Exception) {
            logger.warn("Failed to parse Unity Player: ${e.message}")
        }
        return null
    }

    private fun refreshUnityPlayersList() {
        synchronized(unityPlayerDescriptorsHeartbeats) {
            logger.trace("Refreshing Unity players list...")
            for (playerDescriptor in unityPlayerDescriptorsHeartbeats.keys) {
                val currentPlayerTimeout = unityPlayerDescriptorsHeartbeats[playerDescriptor] ?: continue
                if (currentPlayerTimeout <= 0) {
                    unityPlayerDescriptorsHeartbeats.remove(playerDescriptor)
                    logger.trace("Removing old Unity player $playerDescriptor")
                    unityPlayers.remove(playerDescriptor)?.let { onPlayerRemoved(it) }
                } else
                    unityPlayerDescriptorsHeartbeats[playerDescriptor] = currentPlayerTimeout - 1
            }

            synchronized(socketsLock) {
                for (socket in multicastSockets) {
                    val buf = ByteArray(1024)
                    val recv = DatagramPacket(buf, buf.size)
                    try {
                        if (socket.isClosed)
                            continue
                        socket.receive(recv)
                        val descriptor = String(buf, 0, recv.length - 1)
                        val hostAddress = recv.address.hostAddress
                        logger.trace("Get heartbeat on port ${socket.port} from $hostAddress: $descriptor")
                        if (!unityPlayerDescriptorsHeartbeats.containsKey(descriptor)) {
                            parseUnityPlayer(descriptor, hostAddress)?.let {
                                unityPlayers[descriptor] = it
                                onPlayerAdded(it)
                            }
                        }
                        unityPlayerDescriptorsHeartbeats[descriptor] = defaultHeartbeat
                    } catch (e: SocketTimeoutException) {
                        //wait timeout, go to the next port
                    }
                }
            }
            logger.trace("Finished refreshing of Unity players list.")
        }
    }

    fun close() {
        refreshTimer.cancel()
        synchronized(socketsLock) {
            for (socket in multicastSockets) {
                socket.close()
            }
        }
    }
}