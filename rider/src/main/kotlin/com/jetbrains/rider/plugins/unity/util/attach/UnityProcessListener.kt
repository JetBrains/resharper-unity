package com.jetbrains.rider.plugins.unity.util.attach

import com.intellij.execution.process.OSProcessUtil
import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rider.plugins.unity.util.convertPortToDebuggerPort
import com.jetbrains.rider.run.configurations.remote.Unity.UnityProcessUtil
import java.net.*
import java.util.*
import java.util.regex.Pattern

class UnityProcessListener(private val onPlayerAdded: (UnityPlayer?) -> Unit, private val onPlayerRemoved: (UnityPlayer?) -> Unit) {

    companion object {
        private val logger = Logger.getInstance(UnityProcessListener::class.java)
    }

    private val unityPlayerDescriptorRegex = Pattern.compile("\\[IP\\] (?<ip>.*) \\[Port\\] (?<port>.*) \\[Flags\\] (?<flags>.*) \\[Guid\\] (?<guid>.*) \\[EditorId\\] (?<editorid>.*) \\[Version\\] (?<version>.*) \\[Id\\] (?<id>[^:]+)(:(?<debuggerPort>\\d+))? \\[Debug\\] (?<debug>.*)")

    private val defaultHeartbeat = 30

    private val refreshPeriod: Long = 100
    private val waitOnSocketLength = 50
    private val multicastPorts = listOf(54997, 34997, 57997, 58997)
    private val playerMulticastGroup = "225.0.0.222"
    private val multicastSockets = mutableListOf<MulticastSocket>()

    private val unityPlayerDescriptorsHeartbeats = mutableMapOf<String, Int>()

    private val refreshTimer: Timer
    private val socketsLock = Object()

    init {
        for (networkInterface in NetworkInterface.getNetworkInterfaces()) {
            if (!networkInterface.isUp || !networkInterface.supportsMulticast() || !networkInterface.inetAddresses.hasMoreElements()
                    || networkInterface.inetAddresses.nextElement() is Inet6Address) //TODO: remove this workaround by setting java.net.preferIPv4Stack to true
                continue
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

        refreshTimer = kotlin.concurrent.timer("Listen for Unity Players", true, 0L, refreshPeriod, {
            refreshUnityPlayersList()
        })

        OSProcessUtil.getProcessList().filter { UnityProcessUtil.isUnityEditorProcess(it) }.map { processInfo ->
            val port = convertPortToDebuggerPort(processInfo.pid)
            UnityPlayer("127.0.0.1", port, 0, port.toLong(), port.toLong(), 0, processInfo.executableName, true, port)
        }.forEach {
            onPlayerAdded(it)
        }
    }

    private fun parseUnityPlayer(unityPlayerDescriptor: String): UnityPlayer? {
        try {
            val matcher = unityPlayerDescriptorRegex.matcher(unityPlayerDescriptor)
            if (matcher.find()) {
                val ip = matcher.group("ip")
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
                return UnityPlayer(ip, port, flags, guid, editorGuid, version, id, allowDebugging, debuggerPort)
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
                    onPlayerRemoved(parseUnityPlayer(playerDescriptor))
                } else
                    unityPlayerDescriptorsHeartbeats.put(playerDescriptor, currentPlayerTimeout - 1)
            }

            synchronized(socketsLock) {
                for (socket in multicastSockets) {
                    val buf = ByteArray(1024)
                    val recv = DatagramPacket(buf, buf.size)
                    try {
                        if (socket.isClosed)
                            continue
                        socket.receive(recv)
                        val descriptor = String(buf)
                        logger.trace("Get heartbeat on port ${socket.port}: $descriptor")
                        if (!unityPlayerDescriptorsHeartbeats.containsKey(descriptor)) {
                            onPlayerAdded(parseUnityPlayer(descriptor))
                        }
                        unityPlayerDescriptorsHeartbeats.put(descriptor, defaultHeartbeat)
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