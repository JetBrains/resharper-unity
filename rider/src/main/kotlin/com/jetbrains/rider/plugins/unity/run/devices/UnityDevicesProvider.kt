package com.jetbrains.rider.plugins.unity.run.devices

import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.components.Service
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isNotAlive
import com.jetbrains.rd.util.threading.coroutines.async
import com.jetbrains.rd.util.threading.coroutines.nextFalseValueAsync
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.run.UnityDebuggableDeviceListener
import com.jetbrains.rider.plugins.unity.run.UnityEditorEntryPoint
import com.jetbrains.rider.plugins.unity.run.UnityEditorEntryPointAndPlay
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.run.devices.*
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay

@Service(Service.Level.PROJECT)
class UnityDevicesProvider(private val project: Project): DevicesProvider {
    private val workingTime = 3_000L
    private val locker = Object()
    private val cachedDevices = mutableListOf<UnityProcess>()
    private val deviceKindsForSearch = listOf(UnityUsbDeviceKind, UnityRemotePlayerDeviceKind) // search those

    // Copy of all availableDevices - thread-safe
    private val allDevices
        get() = synchronized(locker) {
            cachedDevices.toList()
        }

    override fun getDeviceKinds(): List<DeviceKind> {
         return listOf(UnityEditorDeviceKind, UnityUsbDeviceKind, UnityRemotePlayerDeviceKind)
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
                        updateDevices(lifetime.createNested(), action)
                        action.stopProgress(project)
                    }
                    finally {
                        lifetime.terminate()
                    }
                }
            }
        }
        val mutableList = mutableListOf<Device>()
        mutableList.add(UnityEditorEntryPoint(UnityBundle.message("unity.editor.devices.kind.name"), -1, project.solutionDirectory.name))
        mutableList.add(UnityEditorEntryPointAndPlay(UnityBundle.message("unity.editor.and.play"), -1, project.solutionDirectory.name))
        mutableList.addAll(allDevices.toHashSet())
        return mutableList
    }

    private suspend fun updateDevices(lifetime: LifetimeDefinition, action: ActiveDeviceAction) {
        // Take a snapshot before starting - will use it later
        val currentlyDiscovered = synchronized(locker) { cachedDevices.toSet() }
        val previouslySeenDevices = currentlyDiscovered.toMutableSet()

        // Listen for workingTime, updating availableDevices as discoveries happen
        UnityDebuggableDeviceListener(
            project, lifetime,
            onProcessAdded = { device ->
                if (deviceKindsForSearch.any { device.kind == it } ) {
                    synchronized(locker) {
                        previouslySeenDevices.remove(device)
                        cachedDevices.remove(device)
                        cachedDevices.add(device)
                    }
                    ActiveDeviceManager.getInstance(project).startRefreshingDevices()
                }
            },
            onProcessRemoved = { device ->
                if (deviceKindsForSearch.any { device.kind == it } ) {
                    synchronized(locker) {
                        cachedDevices.remove(device)
                    }
                    ActiveDeviceManager.getInstance(project).removeDevice(device)
                    ActiveDeviceManager.getInstance(project).startRefreshingDevices()
                }
            }
        )

        coroutineScope {
            action.isExpanded.nextFalseValueAsync(lifetime).await()
            delay(workingTime)
        }

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
