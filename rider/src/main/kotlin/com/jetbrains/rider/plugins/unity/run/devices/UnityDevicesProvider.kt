package com.jetbrains.rider.plugins.unity.run.devices

import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.components.Service
import com.intellij.openapi.diagnostic.runAndLogException
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.Project
import com.intellij.platform.util.progress.reportProgress
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isNotAlive
import com.jetbrains.rd.util.threading.coroutines.async
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.run.UnityDebuggableDeviceListener
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.run.devices.*
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay

@Service(Service.Level.PROJECT)
class UnityDevicesProvider(private val project: Project): DevicesProvider {
    private val workingTime = 3_000L
    private val locker = Object()
    private val cachedDevices = mutableListOf<UnityProcess>()

    // Copy of all availableDevices - thread-safe
    private val allDevices
        get() = synchronized(locker) {
            cachedDevices.toList()
        }

    override fun getDeviceKinds(): List<DeviceKind> {
        return listOf(UnityUsbDeviceKind, UnityRemotePlayerDeviceKind)
    }

    private var lifetime = LifetimeDefinition.Terminated
    // we can't let UnityDebuggableProcessListener run all the time, so
    // it just runs workingTime, collects devices
    // when the device widget is touched, run it again
    // in the worst case there might be a $workingTime seconds non blocking delay with updating devices
    override suspend fun loadAllDevices(): List<Device> {
        if (lifetime.isNotAlive) {
            lifetime = UnityProjectLifetimeService.getNestedLifetimeDefinition(project)
            lifetime.async {
                coroutineScope {
                    try {
                        val action = ActionManager.getInstance().getAction("ActiveDevice") as ActiveDeviceAction
                        action.progress()
                        updateDevices(lifetime.createNested())
                        action.stopProgress(project)
                    }
                    finally {
                        lifetime.terminate()
                    }
                }
            }
        }
        return allDevices
    }

    private suspend fun updateDevices(lifetime: LifetimeDefinition) {
        // Take a snapshot before starting - will use it later
        val currentlyDiscovered = synchronized(locker) { cachedDevices.toSet() }
        val previouslySeenDevices = currentlyDiscovered.toMutableSet()

        // Listen for workingTime, updating availableDevices as discoveries happen
        UnityDebuggableDeviceListener(
            project, lifetime,
            onProcessAdded = { device ->
                if (getDeviceKinds().any { device.kind == it } ) {
                    synchronized(locker) {
                        previouslySeenDevices.remove(device)
                        cachedDevices.remove(device)
                        cachedDevices.add(device)
                    }
                    ActiveDeviceManager.getInstance(project).startRefreshingDevices()
                }
            },
            onProcessRemoved = { device ->
                if (getDeviceKinds().any { device.kind == it } ) {
                    synchronized(locker) {
                        cachedDevices.remove(device)
                    }
                    ActiveDeviceManager.getInstance(project).removeDevice(device)
                    ActiveDeviceManager.getInstance(project).startRefreshingDevices()
                }
            }
        )

        delayWithProgress()

        // Drop devices, which were not seen during this scan
        synchronized(locker) {
            previouslySeenDevices.forEach { device ->
                ActiveDeviceManager.getInstance(project).removeDevice(device)
                ActiveDeviceManager.getInstance(project).removeActiveDevice(device)
            }
            cachedDevices.removeAll(previouslySeenDevices)
        }
        lifetime.terminate()
    }

    private suspend fun delayWithProgress(){
        // using progress reporter to update progress bar, instead of simple `delay(workingTime)`
        val steps = 100
        val delay = (workingTime / steps)
        reportProgress(steps) { reporter ->
            coroutineScope {
                (0 until steps).forEach { i ->
                    reporter.itemStep {
                        delay(delay)
                    }
                }
            }
        }
    }

    override fun checkCompatibility(deviceKind: DeviceKind): CompatibilityProblem? = null
    override fun checkCompatibility(device: Device): CompatibilityProblem? = null

    override fun getDeviceConfigurationActions(): List<AnAction> {
        return listOf(ActionManager.getInstance().getAction("AttachToUnityProcessAction")
        )
    }

    override fun hasSupportForRefreshingDeviceView(): Boolean = false

    companion object {
        fun getService(project: Project): UnityDevicesProvider {
            return project.getService(UnityDevicesProvider::class.java)
        }
    }
}
