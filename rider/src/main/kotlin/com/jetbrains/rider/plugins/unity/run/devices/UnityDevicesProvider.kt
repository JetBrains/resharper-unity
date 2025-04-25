package com.jetbrains.rider.plugins.unity.run.devices

import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.components.Service
import com.intellij.openapi.project.Project
import com.intellij.util.ui.UIUtil
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.run.UnityDebuggableProcessListener
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.run.devices.*
import java.util.concurrent.atomic.AtomicBoolean

@Service(Service.Level.PROJECT)
class UnityDevicesProvider(private val project: Project): DevicesProvider {
    private val locker = Object()
    private val availableDevices = mutableListOf<UnityDevice>()
    private var refreshStartedOnce = AtomicBoolean(false)

    private val allDevices
        get() = mutableListOf<UnityDevice>().apply {
            synchronized(locker) {
                addAll(availableDevices)
            }
        }

    override fun getDeviceKinds(): List<DeviceKind> {
        return listOf(UnityDeviceKind)
    }

    override suspend fun loadAllDevices(): List<Device> {
        val lifetime = UnityProjectLifetimeService.getLifetime(project)
        if (refreshStartedOnce.compareAndSet(false, true)){
            UnityDebuggableProcessListener(project, lifetime,
                                           { UIUtil.invokeLaterIfNeeded { addProcess(it) } },
                                           { UIUtil.invokeLaterIfNeeded { removeProcess(it) } }
            )
        }

        return allDevices
    }

    private fun removeProcess(it: UnityProcess) {
        availableDevices.remove(it.toUnityDevice())
        ActiveDeviceManager.getInstance(project).startRefreshingDevices()
    }

    fun addProcess(it: UnityProcess) {
        availableDevices.add(it.toUnityDevice())
        ActiveDeviceManager.getInstance(project).startRefreshingDevices()
    }

    override fun checkCompatibility(deviceKind: DeviceKind): CompatibilityProblem? = null
    override fun checkCompatibility(device: Device): CompatibilityProblem? = null

    override fun getDeviceConfigurationActions(): List<AnAction> {
        return listOf(ActionManager.getInstance().getAction("AttachToUnityProcessAction")
        )
    }

    companion object {
        fun getService(project: Project): UnityDevicesProvider {
            return project.getService(UnityDevicesProvider::class.java)
        }
    }
}