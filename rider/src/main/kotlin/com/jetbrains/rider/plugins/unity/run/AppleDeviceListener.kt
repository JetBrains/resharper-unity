package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.CapturingProcessHandler
import com.intellij.execution.process.ProcessAdapter
import com.intellij.execution.process.ProcessEvent
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.util.io.isDirectory
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.AssemblyExecutionContext
import com.jetbrains.rider.RiderEnvironment
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import java.io.PrintWriter
import java.nio.file.Path
import java.util.regex.Pattern
import kotlin.concurrent.thread
import kotlin.io.path.exists

class AppleDeviceListener(project: Project,
                          lifetime: Lifetime,
                          private val onDeviceAdded: (UnityProcess) -> Unit,
                          private val onDeviceRemoved: (UnityProcess) -> Unit) {

    companion object {
        private val logger = Logger.getInstance(AppleDeviceListener::class.java)
        private val countRegex = "^(?<count>\\d+)$".toRegex()
        private val deviceRegex = Pattern.compile("""(?<productId>\S*)\s(?<deviceId>.*)""")
    }

    private val refreshPeriod: Long = 1000
    private val devices = mutableMapOf<String, UnityIosUsbProcess>()
    private val usage = mutableMapOf<String, Boolean>()
    private val thread: Thread?
    private val processHandler: CapturingProcessHandler?
    private val descriptions: Map<Int, String>

    // Fetch the list of Apple devices currently attached via USB. This is done using Apple's usbmuxd service, which
    // is cross-platform and is essentially a socket based service that can send and receive plist messages. It can
    // iterate the currently attached devices, create a socket to proxy data to/from a socket on the device, and
    // loads more.
    // Unity provides a native library that we can load and use the exported functions to get the devices and manage
    // the proxy. Sadly, we can't load that with JNA, as it doesn't unload and reload cleanly. And we need to unload
    // it as it is release specific (although probably hasn't changed for years). This only gives us device and
    // product IDs. We need the device ID to set up the proxy and the product ID is to look up a name in a csv file,
    // which is sadly not enough - the product ID 12a8 is used for iPhones 5 - 9. Best we can do is get the type,
    // e.g. iPhone, iPad Pro, etc.
    // We could potentially use JBDeviceFramework to get more details (such as device name and checking running
    // processes for an actual player), but AFAICT it only runs on Mac, and requires Xcode installed. Alternatively, we
    // could use libimobiledevice, but that seems like overkill, as it supports lots of different services over usbmuxd.
    // Finally, we could roll our own. Trickiest part is likely plist handling, and we might be able to reuse something
    // in the platform for that.
    init {
        var varProcessHandler: CapturingProcessHandler? = null
        var varDescriptions = emptyMap<Int, String>()

        try {
            val iosSupportPath = UnityInstallationFinder.getInstance(project).getAdditionalPlaybackEnginesRoot()?.resolve("iOSSupport")
            if (iosSupportPath != null && iosSupportPath.isDirectory()) {
                logger.trace("Using iOSSupportPath: $iosSupportPath")

                varDescriptions = loadDescriptions(iosSupportPath)
                varProcessHandler = startListener(iosSupportPath)
            } else {
                logger.warn("Cannot find $iosSupportPath folder")
            }
        } catch (e: Throwable) {
            logger.error(e)
        }

        descriptions = varDescriptions

        processHandler = varProcessHandler
        thread = if (processHandler != null) {
            thread { processHandler.runProcess() }
        } else {
            null
        }

        lifetime.onTermination { stop() }
    }

    private fun stop() {
        if (thread == null || processHandler == null) {
            return
        }

        // The process should stop polling and exit when it receives "stop\n"

        logger.trace("Telling ListIosUsbDevices helper exe to stop")
        processHandler.processInput.let {
            PrintWriter(it, true).println("stop")
        }

        // It checks the flag when it polls
        val threadTimeout = refreshPeriod * 200
        try {
            thread.join(threadTimeout)
        }
        catch(e: Throwable) {
            logger.error(e)
        }

        if (thread.isAlive) {
            logger.warn("ListIosUsbDevices helper exe didn't return in ${threadTimeout}ms. Killing")
            processHandler.destroyProcess()
        }
    }

    @Suppress("SpellCheckingInspection")
    private fun loadDescriptions(iosSupportPath: Path): Map<Int, String> {
        val file = iosSupportPath.resolve("Data/iosdevices.csv")
        return if (file.exists()) {
            logger.trace("Loading iOS device descriptions from $file")

            val map = mutableMapOf<Int, String>()
            try {
                file.toFile().forEachLine {
                    // # VendorId;ProductId;Revision;ModelId;Type;Model;Comment
                    // VendorId is always 05ac, which is Apple
                    if (!it.startsWith("#")) {
                        val columns = it.split(';')
                        if (columns.size >= 4) {
                            val productId = columns[1].let { v -> if (v.isNotEmpty()) v.toInt(16) else 0 }
                            val type = columns[4]

                            // We only have productId and that's not enough to give a decent description. We'd need
                            // revisionId to stand a chance of using model (columns[5]), but even that's not fully
                            // populated in the iosdevices.csv file
                            // TODO: Consider implementing the usbmuxd protocol ourselves
                            // This could give us more information, including device name

                            if (productId != 0) {
                                map[productId] = type
                            }
                        }
                    }
                }
            } catch (e: Throwable) {
                logger.error(e)
            }
            map
        }
        else {
            logger.warn("Cannot find iOS device description at $file")
            emptyMap()
        }
    }

    private fun startListener(iosSupportPath: Path): CapturingProcessHandler {
        // Get the helper exe from the DotFiles folder. TBH, I suspect the 'DotFiles' name is incorrect, as Rider plugin
        // files (including the 'Extensions' folder) live under 'dotnet'. ReSharper plugins ship in a 'DotFiles' folder,
        // but are installed into the main install folder. No-one actually uses 'DotFiles' now
        val helperExe = RiderEnvironment.getBundledFile("JetBrains.Rider.Unity.ListIosUsbDevices.dll", pluginClass = javaClass)
        val commandLine = AssemblyExecutionContext(helperExe, RiderEnvironment.customExecutionOs, null,
            iosSupportPath.toString(), "$refreshPeriod").fillCommandLine(GeneralCommandLine())
        val processHandler = CapturingProcessHandler(commandLine)
        val rawDevices = mutableListOf<String>()
        processHandler.addProcessListener(object : ProcessAdapter() {
            var expectedCount = 0
            var unexpectedText = ""

            override fun processTerminated(event: ProcessEvent) {
                logger.trace("Helper process exited. Exit code: ${event.exitCode}")

                if (unexpectedText.isNotEmpty()) {
                    logger.error("Error running $helperExe. Output:\n$unexpectedText")
                }
            }

            override fun onTextAvailable(event: ProcessEvent, key: Key<*>) {
                // Text is split by lines and includes the trailing newline
                // Unity's dll will output status messages beginning with `[usbmuxd`. We'll ignore these
                // Then the helper app will output a device count on a single line, followed by each device, with a line
                // per device, in the format `product-id device-id`. (Product ID is in hex)
                logger.debug("Received text: ${event.text?.trimEnd()}")

                if (!event.text.startsWith("[usbmuxd")) {
                    // Are we currently expecting a device text, or a new count?
                    if (expectedCount == 0) {

                        // Expecting a new count
                        val match = countRegex.find(event.text)
                        if (match != null) {
                            unexpectedText = ""

                            expectedCount = match.groups["count"]?.value?.toInt() ?: 0

                            // There are no devices connected, but we still think there are. Process our (empty) list to
                            // clean up
                            if (expectedCount == 0 && devices.isNotEmpty()) {
                                processDevices(rawDevices)
                            }
                        }
                        else {
                            unexpectedText += event.text
                        }
                    } else {
                        // We're expecting a device text. Read it and decrement our expected count. When the expected
                        // count hits zero, process our collected list of device texts
                        rawDevices.add(event.text)
                        if (--expectedCount == 0) {
                            logger.trace("Refreshing Apple USB devices: ${rawDevices.size}")
                            processDevices(rawDevices)
                            rawDevices.clear()
                        }
                    }
                }
            }
        })

        return processHandler
    }

    private fun processDevices(rawDevices: List<String>) {
        usage.keys.forEach { usage[it] = false }

        val newProcesses = rawDevices.mapNotNull {
            val matcher = deviceRegex.matcher(it)
            if (matcher.find()) {
                val productId = matcher.group("productId").toInt(16)
                val deviceId = matcher.group("deviceId")

                val displayName = descriptions[productId] ?: "Apple Device"

                if (!devices.containsKey(deviceId)) {
                    val process = UnityIosUsbProcess(displayName, deviceId)
                    devices[deviceId] = process
                    usage[deviceId] = true
                    process
                }
                else {
                    usage[deviceId] = true
                    null
                }
            }
            else {
                logger.warn("Expected device text. Could not match: $it")
                null
            }
        }

        newProcesses.forEach { onDeviceAdded(it) }
        usage.filterValues { !it }.keys.forEach { deviceId ->
            usage.remove(deviceId)
            devices.remove(deviceId)?.let { onDeviceRemoved(it) }
        }
    }
}
