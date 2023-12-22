package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import kotlinx.coroutines.delay
import java.net.*
import java.nio.ByteBuffer
import java.nio.channels.DatagramChannel
import java.nio.channels.SelectionKey
import java.nio.channels.Selector
import java.util.regex.Pattern

class UnityPlayerListener {

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
  (\s\[ProjectName]\s(?<projectName>.*))
)?
""", Pattern.COMMENTS)

        @Suppress("unused")
        enum class UnityPlayerConnectionFlags(val value: Int) {
            RequestImmediateConnect(1 shl 0),
            SupportsProfile(1 shl 1),
            CustomMessage(1 shl 2),
            UseAlternateIP(1 shl 3);

            fun isSet(value: Int): Boolean = this.value and value != 0
        }
    }

    // Refresh once a second. If a player hasn't been seen for 3 iterations, remove it from the list
    private val refreshPeriod: Long = 1000
    private val defaultHeartbeat = 3

    private val multicastPorts = listOf(54997, 34997, 57997, 58997)
    private val playerMulticastGroup = InetAddress.getByName("225.0.0.222")
    private val selector = Selector.open()

    private val unityPlayerDescriptorsHeartbeats = mutableMapOf<String, Int>()
    private val unityProcesses = mutableMapOf<String, UnityProcess>()

    private val syncLock = Object()

    // Invoked on the UI thread
    fun startListening(lifetime: Lifetime,
                       onPlayerAdded: (UnityProcess) -> Unit,
                       onPlayerRemoved: (UnityProcess) -> Unit) {

        startListeningUdp(lifetime)

        val refreshTimer = kotlin.concurrent.timer("Listen for Unity Players", true, 0L, refreshPeriod) {
            refreshUnityPlayersList(onPlayerAdded, onPlayerRemoved)
        }
        lifetime.onTermination { refreshTimer.cancel() }
    }

    // Invoked on a background thread
    suspend fun getPlayer(project: Project, predicate: (UnityProcess) -> Boolean): UnityProcess? {
        val lifetime = UnityProjectLifetimeService.getLifetime(project).createNested()
        try {
            startListeningUdp(lifetime)

            val players = mutableListOf<UnityProcess>()

            // Give all players the chance to broadcast, and keep going until we find our player, or we time out.
            // We repeat 30 times, with a 100ms delay, so we'll wait 3 second for the player
            for (i in 1..30) {
                refreshUnityPlayersList({ players.add(it) }, { players.remove(it) })
                val player = players.firstOrNull(predicate)
                if (player != null) {
                    return player
                }

                delay(100)
            }

            return null
        }
        finally {
            lifetime.terminate()
        }
    }

    private fun startListeningUdp(lifetime: Lifetime) {
        for (networkInterface in NetworkInterface.getNetworkInterfaces()) {
            if (!networkInterface.isUp || !networkInterface.supportsMulticast()
                || !networkInterface.inetAddresses.asSequence().any { it is Inet4Address }) {
                continue
            }

            multicastPorts.forEach { port ->
                try {
                    // Setting the network interface will set the first IPv4 address on the socket's fd
                    val channel = DatagramChannel.open(StandardProtocolFamily.INET)
                        .setOption(StandardSocketOptions.SO_REUSEADDR, true)
                        .setOption(StandardSocketOptions.IP_MULTICAST_IF, networkInterface)
                        .bind(InetSocketAddress(port))
                    channel.configureBlocking(false)
                    channel.join(playerMulticastGroup, networkInterface)
                    channel.register(selector, SelectionKey.OP_READ)
                } catch (e: Exception) {
                    logger.warn(e.message)
                }
            }
        }

        lifetime.onTermination {
            synchronized(syncLock) {
                selector.keys().forEach {
                    try {
                        // Close the channel. This will cancel the selection key and removes multicast group membership.
                        // It doesn't close the socket, as there are still selector registrations active
                        it.channel().close()
                    } catch (e: Throwable) {
                        logger.warn(e)
                    }
                }

                // Close the selector. This de-registers the selector from all channels, and then kills the socket
                // attached to the already closed channel
                selector.close()
            }
        }
    }

    private fun parseUnityPlayer(unityPlayerDescriptor: String, packetSourceAddress: InetAddress): UnityProcess? {
        try {
            val matcher = unityPlayerDescriptorRegex.matcher(unityPlayerDescriptor)
            if (matcher.find()) {
                val ipInMessage = matcher.group("ip")
                val id = matcher.group("id")
                val flags = matcher.group("flags").toInt()
                val allowDebugging = matcher.group("debug").startsWith("1")
                val guid = matcher.group("guid").toLong()
                val debuggerPort = matcher.group("debuggerPort")?.toIntOrNull() ?: convertPidToDebuggerPort(guid)
                val packageName: String? = matcher.group("packageName")
                val projectName: String? = matcher.group("projectName")

                // If UseAlternateIP is set that means we should use the IP in the broadcast message rather than the
                // packet source address
                val hostAddress = when {
                    UnityPlayerConnectionFlags.UseAlternateIP.isSet(flags) -> InetAddress.getByName(ipInMessage)
                    else -> packetSourceAddress
                }

                return if (isLocalAddress(hostAddress)) {
                    if (id.startsWith(UnityLocalUwpPlayer.TYPE) && packageName != null) {
                        UnityLocalUwpPlayer(id, hostAddress.hostAddress, debuggerPort, allowDebugging, projectName, packageName)
                    }
                    else {
                        UnityLocalPlayer(id, hostAddress.hostAddress, debuggerPort, allowDebugging, projectName)
                    }
                }
                else {
                    UnityRemotePlayer(id, hostAddress.hostAddress, debuggerPort, allowDebugging, projectName)
                }
            }
        } catch (e: Exception) {
            logger.warn(e)
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

    private fun refreshUnityPlayersList(
        onPlayerAdded: (UnityProcess) -> Unit,
        onPlayerRemoved: (UnityProcess) -> Unit
    ) {
        synchronized(syncLock) {

            val start = System.currentTimeMillis()
            logger.trace("Refreshing Unity players list...")

            for (playerDescriptor in unityPlayerDescriptorsHeartbeats.keys) {
                val currentPlayerTimeout = unityPlayerDescriptorsHeartbeats[playerDescriptor] ?: continue
                unityPlayerDescriptorsHeartbeats[playerDescriptor] = currentPlayerTimeout - 1
            }

            unityPlayerDescriptorsHeartbeats.filterValues { it <= 0 }.keys.forEach { playerDescription ->
                unityPlayerDescriptorsHeartbeats.remove(playerDescription)
                logger.trace("Removing old Unity player $playerDescription")
                unityProcesses.remove(playerDescription)?.let { onPlayerRemoved(it) }
            }

            // Read all data from all channels that is currently available. There might be more than one message ready
            // on a channel as players continuously send multicast messages, so make sure we read them all. If there is
            // nothing available, we return immediately and start sleeping.
            val buffer = ByteBuffer.allocate(1024)
            while (true) {
                val readyChannels = selector.selectNow { key ->
                    try {
                        buffer.clear()

                        val channel = key.channel() as DatagramChannel
                        val hostAddress = channel.receive(buffer) as InetSocketAddress

                        // The UDP message may or may not be already null terminated. Unity players seem to all include
                        // it, but the support script doesn't. We shouldn't rely on it being there.
                        val descriptor = String(buffer.array(), 0, buffer.position()).trimEnd('\u0000')

                        if (logger.isTraceEnabled) {
                            logger.trace("Got heartbeat on ${channel.remoteAddress} from $hostAddress: $descriptor")
                        }

                        if (!unityPlayerDescriptorsHeartbeats.containsKey(descriptor)) {
                            parseUnityPlayer(descriptor, hostAddress.address)?.let { player ->
                                unityProcesses[descriptor] = player
                                onPlayerAdded(player)
                            }
                        }

                        unityPlayerDescriptorsHeartbeats[descriptor] = defaultHeartbeat
                    } catch (e: Exception) {
                        logger.warn(e)
                    }
                }

                if (readyChannels == 0) {
                    break
                }
            }

            if (logger.isTraceEnabled) {
                val duration = System.currentTimeMillis() - start
                logger.trace("Finished refreshing Unity players list. Took ${duration}ms")
            }
        }
    }
}