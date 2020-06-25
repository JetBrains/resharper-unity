package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.process.OSProcessUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.onTermination
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import java.net.*
import java.util.*
import java.util.regex.Pattern


class UnityPlayerListener(private val project: Project, lifetime: Lifetime,
                          private val onPlayerRefresh: (Boolean) -> Unit,
                          private val onPlayerAdded: (UnityProcess) -> Unit,
                          private val onPlayerRemoved: (UnityProcess) -> Unit) {

    companion object {
        private val logger = Logger.getInstance(UnityPlayerListener::class.java)

        // E.g.:
        // [IP] 10.211.55.4 [Port] 55376 [Flags] 3 [Guid] 1410689715 [EditorId] 1006284310 [Version] 1048832 [Id] WindowsPlayer(PARALLELS) [Debug] 1 [PackageName] WindowsPlayer [ProjectName] GemShader is awesome
        // As far as I can tell:
        // * [IP] %s - where the process is running. On iPhone (and perhaps other devices) this can be the mobile data
        //             IP, which might be unreachable from this subnet, so be prepared to use the IP address from the
        //             UDP packet instead
        // * [Port] %u - NOT the debugging port. I think this port connects to the player (to e.g. get logs)
        // * [Flags] %u - settings for the editor. Don't know what the values are
        // * [Guid] %u - random number. Consistent only for the lifetime of the player. If no debugging port is found as
        //               part of `id`, then the debugging port is `guid % 1000 + 56000` (which belies the part that it's
        //               a random number, and is more likely that if no debugging port is specified, this must be a PID)
        // * [EditorId] %u - random number representing an ID of the editor instance that built this player. Consistent
        //                   for the lifetime of the editor. Will also be used by any other player built by the same
        //                   editor instance
        // * [Version] %d - static value. Never been changed
        // * [Id] %s - a textual identifier, e.g. "OSXPlayer(Matts.MacBookPro.Local)". May also include debugging port,
        //             e.g. `iPhonePlayer(Matts.iPhone7):56000`
        // * [Debug] %d - 0 or 1 to show if debugging is enabled. Will not be able to attach if this is 0
        // * [PackageName] %s - the type of the player, e.g. `iPhonePlayer` or `WindowsPlayer`. Could be used as as a
        //                      "type" field. This is not present in all messages, so I guess it was added in a specific
        //                      version of Unity, but don't know which
        // * [ProjectName] %s - same as PlayerSettings.productName. Added in Unity 2019.3a6
        @Suppress("RegExpRepeatedSpace")
        private val unityPlayerDescriptorRegex = Pattern.compile("""\[IP]\s(?<ip>.*)
\s\[Port]\s(?<port>\d+)
\s\[Flags]\s(?<flags>\d+)
\s\[Guid]\s(?<guid>\d+)
\s\[EditorId]\s(?<editorId>\d+)
\s\[Version]\s(?<version>\d+)
\s\[Id]\s(?<id>[^:]+)(:(?<debuggerPort>\d+))?
\s\[Debug]\s(?<debug>\d+)
(\s\[PackageName]\s(?<packageName>.*?)
  (\s\[ProjectName]\s(?<projectName>.*)?)
)?
""", Pattern.COMMENTS)
    }

    // Refresh once a second. If a player hasn't been seen for 3 iterations, remove it from the list
    private val refreshPeriod: Long = 1000
    private val defaultHeartbeat = 3

    private val waitOnSocketLength = 50
    private val multicastPorts = listOf(54997, 34997, 57997, 58997)
    private val playerMulticastGroup = "225.0.0.222"
    private val multicastSockets = mutableListOf<MulticastSocket>()

    private val unityPlayerDescriptorsHeartbeats = mutableMapOf<String, Int>()
    private val unityProcesses = mutableMapOf<String, UnityProcess>()

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

        onPlayerRefresh(true)
        addLocalProcesses()
        onPlayerRefresh(false)

        refreshTimer = startAddingPlayers()

        lifetime.onTermination {
            close()
        }
    }

    private fun addLocalProcesses() {
        val unityProcesses = OSProcessUtil.getProcessList().filter { UnityRunUtil.isUnityEditorProcess(it) }
        val unityProcessInfoMap = UnityRunUtil.getAllUnityProcessInfo(unityProcesses, project)
        unityProcesses.map { processInfo ->
            val unityProcessInfo = unityProcessInfoMap[processInfo.pid]

            if (!unityProcessInfo?.roleName.isNullOrEmpty()) {
                UnityEditorHelper(processInfo.executableName, unityProcessInfo?.roleName!!, processInfo.pid, unityProcessInfo.projectName)
            }
            else {
                UnityEditor(processInfo.executableName, processInfo.pid, unityProcessInfo?.projectName)
            }
        }.forEach {
            onPlayerAdded(it)
        }
    }

    private fun startAddingPlayers(): Timer {
        return kotlin.concurrent.timer("Listen for Unity Players", true, 0L, refreshPeriod) {
            refreshUnityPlayersList()
        }
    }

    private fun parseUnityPlayer(unityPlayerDescriptor: String, hostAddress: InetAddress): UnityProcess? {
        try {
            val matcher = unityPlayerDescriptorRegex.matcher(unityPlayerDescriptor)
            if (matcher.find()) {
                val id = matcher.group("id")
                val allowDebugging = matcher.group("debug").startsWith("1")
                val guid = matcher.group("guid").toLong()
                val debuggerPort = matcher.group("debuggerPort")?.toIntOrNull() ?: convertPidToDebuggerPort(guid)
                val projectName: String? = matcher.group("projectName")

                // We use hostAddress instead of ip because this is the address we actually received the multicast from.
                // This is more accurate than what we're told, because the Unity process might be reporting the IP
                // address of an interface that isn't reachable. For example, the iPhone player can report the local IP
                // address of the mobile data network, which we can't reach from the current network (if we disable
                // mobile data it works as expected)
                return if (isLocalAddress(hostAddress)) {
                    UnityLocalPlayer(id, hostAddress.hostAddress, debuggerPort, allowDebugging, projectName)
                }
                else {
                    UnityRemotePlayer(id, hostAddress.hostAddress, debuggerPort, allowDebugging, projectName)
                }
            }
        } catch (e: Exception) {
            logger.warn("Failed to parse Unity Player: ${e.message}")
        }
        return null
    }

    // https://stackoverflow.com/questions/2406341/how-to-check-if-an-ip-address-is-the-local-host-on-a-multi-homed-system
    private fun isLocalAddress(addr: InetAddress): Boolean {
        // Check if the address is a valid special local or loop back
        return if (addr.isAnyLocalAddress || addr.isLoopbackAddress) true else try {
            // Check if the address is defined on any interface
            NetworkInterface.getByInetAddress(addr) != null
        } catch (e: SocketException) {
            false
        }
    }

    private fun refreshUnityPlayersList() {
        synchronized(unityPlayerDescriptorsHeartbeats) {

            onPlayerRefresh(true)

            val start = System.currentTimeMillis()
            logger.trace("Refreshing Unity players list...")
            try {
                // TODO: Seen a ConcurrentModificationException here
                // I don't think it's legal to remove while enumerating
                for (playerDescriptor in unityPlayerDescriptorsHeartbeats.keys) {
                    val currentPlayerTimeout = unityPlayerDescriptorsHeartbeats[playerDescriptor] ?: continue
                    if (currentPlayerTimeout <= 0) {
                        unityPlayerDescriptorsHeartbeats.remove(playerDescriptor)
                        logger.trace("Removing old Unity player $playerDescriptor")
                        unityProcesses.remove(playerDescriptor)?.let { onPlayerRemoved(it) }
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
                            val hostAddress = recv.address
                            logger.trace("Get heartbeat on ${socket.networkInterface.name}${socket.`interface`}:${socket.localPort} from $hostAddress: $descriptor")
                            if (!unityPlayerDescriptorsHeartbeats.containsKey(descriptor)) {
                                parseUnityPlayer(descriptor, hostAddress)?.let {
                                    unityProcesses[descriptor] = it
                                    onPlayerAdded(it)
                                }
                            }
                            unityPlayerDescriptorsHeartbeats[descriptor] = defaultHeartbeat
                        } catch (e: SocketTimeoutException) {
                            //wait timeout, go to the next port
                            logger.debug("Socket timed out: ${socket.networkInterface.name}${socket.`interface`}:${socket.localPort}")
                        }
                    }
                }

                if (logger.isTraceEnabled) {
                    val duration = System.currentTimeMillis() - start
                    logger.trace("Finished refreshing of Unity players list. Took ${duration}ms")
                }
            }
            finally {
                onPlayerRefresh(false)
            }
        }
    }

    private fun close() {
        refreshTimer.cancel()
        synchronized(socketsLock) {
            for (socket in multicastSockets) {
                socket.close()
            }
        }
    }
}