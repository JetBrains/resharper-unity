package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.application.EDT
import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.Service
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.setSuspendPreserveClientId
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.wm.WindowManager
import com.intellij.util.BitUtil
import com.jetbrains.rd.framework.impl.RdTask
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.configurations.attachToUnityEditor
import com.jetbrains.rider.plugins.unity.run.configurations.isAttachedToUnityEditor
import com.jetbrains.rider.plugins.unity.util.Utils.Companion.AllowUnitySetForegroundWindow
import com.jetbrains.rider.projectView.solution
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.awt.Frame
import java.io.File
import kotlin.math.max

@Service(Service.Level.PROJECT)
class FrontendBackendHost(project: Project) : LifetimedService() {
    val model = project.solution.frontendBackendModel

    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        private fun activateRider(project: Project) {
            ProjectUtil.focusProjectWindow(project, true)
            val frame = WindowManager.getInstance().getFrame(project)
            if (frame != null) {
                if (BitUtil.isSet(frame.extendedState, Frame.ICONIFIED))
                    frame.extendedState = BitUtil.set(frame.extendedState, Frame.ICONIFIED, false)
            }
        }

        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.activateRider.advise(lifetime) {
                activateRider(session.project)
            }

            model.startUnity.advise(lifetime) {
                StartUnityAction.startUnity(session.project)
            }

            model.attachDebuggerToUnityEditor.set { lt, _ ->
                if (isAttachedToUnityEditor(session.project)) {
                    return@set RdTask.fromResult(true)
                }

                RdTask<Boolean>().also { task ->
                    val processTracker: RiderDebugActiveDotNetSessionsTracker =
                        RiderDebugActiveDotNetSessionsTracker.getInstance(session.project)
                    processTracker.dotNetDebugProcesses.change.advise(lt) { (event, debugProcess) ->
                        if (event == AddRemove.Add) {
                            debugProcess.initializeDebuggerTask.debuggerInitializingState.advise(lt) {
                                if (it == DebuggerInitializingState.Initialized) task.set(true)
                                if (it == DebuggerInitializingState.Canceled) task.set(false)
                            }
                        }
                    }

                    // Run an existing "Attach to Unity Editor" configuration, or create it, and attach
                    attachToUnityEditor(session.project)
                }
            }

            model.allowSetForegroundWindow.set { _, _ ->
                val task = RdTask<Boolean>()

                val id = model.unityApplicationData.valueOrNull?.unityProcessId
                if (id == null)
                    task.set(false)
                else
                    task.set(AllowUnitySetForegroundWindow(id))

                task
            }

            model.openFileLineCol.setSuspendPreserveClientId { _, arg ->
                val manager = FileEditorManager.getInstance(session.project)
                withContext(Dispatchers.IO) {
                    val file = VfsUtil.findFileByIoFile(File(arg.path), true) ?: return@withContext
                    val descriptor = OpenFileDescriptor(session.project, file, max(0, arg.line - 1), max(0, arg.col - 1))
                    withContext(Dispatchers.EDT) {
                        manager.openEditor(descriptor, true)
                    }
                    activateRider(session.project)
                }

                true
            }
        }
    }

    companion object {
        fun getInstance(project: Project): FrontendBackendHost = project.getService(FrontendBackendHost::class.java)
    }
}

fun Project?.isConnectedToEditor() = this != null && this.solution.frontendBackendModel.unityEditorConnected.valueOrDefault(false)